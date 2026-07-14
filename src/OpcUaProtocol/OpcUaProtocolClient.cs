// =============================================================================
// MAS.Communication
// https://www.mas-automation.com/
//
// Copyright 2026 MAS (厦门威光) Corporation
//
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.
// =============================================================================

using Opc.Ua;
using Opc.Ua.Client;
using System.Text;

// TODO: 使用官方客户端的稳定同步友好重载；对应的遥测（ITelemetryContext）新 API 暂不在首版范围内
#pragma warning disable CS0618

namespace MAS.Communication.OpcUaProtocol;

/// <summary>
/// OPC UA 客户端实现，将官方 SDK 的会话/订阅/安全模型映射到统一的 <see cref="IProtocol"/> 生命周期
/// </summary>
internal sealed class OpcUaProtocolClient(IOpcUaCommunicationConfig config) : IOpcUaProtocol {
    private readonly IOpcUaCommunicationConfig _config = config.Clone<IOpcUaCommunicationConfig>();
    private readonly SemaphoreSlim _connectLock = new(1, 1);
    private readonly object _reconnectLock = new();
    private readonly List<OpcUaSubscription> _subscriptions = [];

    private ApplicationConfiguration? _appConfig;
    private Session? _session;
    private SessionReconnectHandler? _reconnectHandler;
    private volatile bool _keepAliveOk;
    private bool _disposed;

    public event EventHandler? Disposed;

    public CommProtocol ProtocolType => CommProtocol.OpcUa;

    public ICommunicationConfig Configuration => _config;

    private string InstanceKey => _config.GetInstanceKey();

    public async Task ConnectAsync(CancellationToken cts = default) {
        ThrowIfDisposed();

        await _connectLock.WaitAsync(cts).ConfigureAwait(false);
        try {
            await EnsureApplicationAsync().ConfigureAwait(false);
            await EstablishSessionAsync(cts).ConfigureAwait(false);
        } catch (OperationCanceledException) {
            throw;
        } catch (ConnectionException) {
            throw;
        } catch (Exception ex) {
            throw new ConnectionException($"{InstanceKey}: OPC UA 连接失败：{ex.Message}", ex);
        } finally {
            _ = _connectLock.Release();
        }
    }

    public async Task<bool> ProbeConnectionAsync(CancellationToken cts = default) {
        ThrowIfDisposed();

        Session? probe = null;
        try {
            await _connectLock.WaitAsync(cts).ConfigureAwait(false);
            try {
                await EnsureApplicationAsync().ConfigureAwait(false);
            } finally {
                _ = _connectLock.Release();
            }

            ConfiguredEndpoint endpoint = await OpcUaEndpointSelector
                .SelectAsync(_appConfig!, _config, cts).ConfigureAwait(false);
            IUserIdentity identity = await BuildIdentityAsync(cts).ConfigureAwait(false);

            probe = await Session.Create(
                _appConfig!,
                endpoint,
                false,
                false,
                _config.ApplicationName + "-probe",
                (uint)_config.SessionTimeout,
                identity,
                null,
                cts).ConfigureAwait(false);

            return true;
        } catch (OperationCanceledException) {
            throw;
        } catch {
            return false;
        } finally {
            if (probe is not null) {
                try {
                    _ = await probe.CloseAsync(cts).ConfigureAwait(false);
                } catch {
                    // 探测阶段忽略关闭异常
                }

                probe.Dispose();
            }
        }
    }

    public bool CheckConnection() {
        Session? session = _session;
        return session is { Connected: true, KeepAliveStopped: false } && _keepAliveOk;
    }

    public async Task<bool> TryReconnectAsync(int maxAttempts, int retryDelay = 1000, CancellationToken cts = default) {
        ThrowIfDisposed();

        await _connectLock.WaitAsync(cts).ConfigureAwait(false);
        try {
            for (int attempt = 0; attempt < maxAttempts; attempt++) {
                cts.ThrowIfCancellationRequested();

                try {
                    await EnsureApplicationAsync().ConfigureAwait(false);
                    await EstablishSessionAsync(cts).ConfigureAwait(false);

                    if (CheckConnection()) {
                        return true;
                    }
                } catch (OperationCanceledException) {
                    throw;
                } catch {
                    // 单次尝试失败，等待后重试
                }

                await Task.Delay(retryDelay, cts).ConfigureAwait(false);
            }

            return false;
        } finally {
            _ = _connectLock.Release();
        }
    }

    public void Disconnect() {
        StopReconnect();

        _connectLock.Wait();
        try {
            CloseSessionInternal();
        } finally {
            _ = _connectLock.Release();
        }
    }

    public void Dispose() {
        if (_disposed) {
            return;
        }

        _disposed = true;
        StopReconnect();

        List<OpcUaSubscription> subscriptions;
        lock (_subscriptions) {
            subscriptions = [.. _subscriptions];
            _subscriptions.Clear();
        }

        foreach (OpcUaSubscription subscription in subscriptions) {
            subscription.Closed -= OnSubscriptionClosed;
            try {
                subscription.Dispose();
            } catch {
                // 释放阶段忽略订阅释放异常
            }
        }

        CloseSessionInternal();
        _connectLock.Dispose();

        Disposed?.Invoke(this, EventArgs.Empty);
    }

    public async Task<OpcUaValue> ReadAsync(OpcUaNodeId nodeId, CancellationToken cancellationToken = default) {
        ThrowIfDisposed();
        Session session = GetActiveSession();

        try {
            ReadValueIdCollection toRead = [
                new ReadValueId { NodeId = OpcUaValueConverter.ToNodeId(nodeId), AttributeId = Attributes.Value }
            ];

            ReadResponse response = await session
                .ReadAsync(null, 0, TimestampsToReturn.Both, toRead, cancellationToken)
                .ConfigureAwait(false);

            OpcUaValue value = OpcUaValueConverter.ToOpcUaValue(nodeId, response.Results[0]);
            if (value.IsBad) {
                throw new ReadErrorException(
                    $"{InstanceKey}: 读取节点失败。NodeId={nodeId.Value}, StatusCode=0x{value.StatusCode:X8}。");
            }

            return value;
        } catch (OperationCanceledException) {
            throw;
        } catch (ReadErrorException) {
            throw;
        } catch (Exception ex) {
            throw new ReadErrorException($"{InstanceKey}: 读取节点失败。NodeId={nodeId.Value}：{ex.Message}", ex);
        }
    }

    public async Task<T?> ReadAsync<T>(OpcUaNodeId nodeId, CancellationToken cancellationToken = default) {
        OpcUaValue value = await ReadAsync(nodeId, cancellationToken).ConfigureAwait(false);

        try {
            return OpcUaValueConverter.ConvertValue<T>(value.Value);
        } catch (Exception ex) {
            throw new ReadErrorException(
                $"{InstanceKey}: 读取节点值类型转换失败。NodeId={nodeId.Value}, TargetType={typeof(T).Name}：{ex.Message}", ex);
        }
    }

    public async Task<IReadOnlyList<OpcUaValue>> ReadAsync(IReadOnlyList<OpcUaReadItem> items, CancellationToken cancellationToken = default) {
        ThrowIfDisposed();
        Session session = GetActiveSession();

        if (items.Count == 0) {
            return [];
        }

        try {
            ReadValueIdCollection toRead = [];
            foreach (OpcUaReadItem item in items) {
                toRead.Add(new ReadValueId { NodeId = OpcUaValueConverter.ToNodeId(item.NodeId), AttributeId = Attributes.Value });
            }

            ReadResponse response = await session
                .ReadAsync(null, 0, TimestampsToReturn.Both, toRead, cancellationToken)
                .ConfigureAwait(false);

            DataValueCollection results = response.Results;
            if (results is null || results.Count != items.Count) {
                throw new ReadErrorException(
                    $"{InstanceKey}: 批量读取返回结果数量不匹配。Expected={items.Count}, Actual={results?.Count ?? 0}。");
            }

            List<OpcUaValue> values = new(items.Count);
            for (int i = 0; i < items.Count; i++) {
                values.Add(OpcUaValueConverter.ToOpcUaValue(items[i].NodeId, results[i]));
            }

            return values;
        } catch (OperationCanceledException) {
            throw;
        } catch (ReadErrorException) {
            throw;
        } catch (Exception ex) {
            throw new ReadErrorException($"{InstanceKey}: 批量读取失败。Count={items.Count}：{ex.Message}", ex);
        }
    }

    public async Task WriteAsync(OpcUaNodeId nodeId, object? value, CancellationToken cancellationToken = default) {
        ThrowIfDisposed();
        Session session = GetActiveSession();

        try {
            WriteValueCollection toWrite = [
                new WriteValue {
                    NodeId = OpcUaValueConverter.ToNodeId(nodeId),
                    AttributeId = Attributes.Value,
                    Value = new DataValue(value is null ? Variant.Null : new Variant(value))
                }
            ];

            WriteResponse response = await session
                .WriteAsync(null, toWrite, cancellationToken)
                .ConfigureAwait(false);

            OpcUaWriteResult result = new(nodeId, response.Results[0].Code);
            if (result.IsBad) {
                throw new WriteErrorException(
                    $"{InstanceKey}: 写入节点失败。NodeId={nodeId.Value}, StatusCode=0x{result.StatusCode:X8}。");
            }
        } catch (OperationCanceledException) {
            throw;
        } catch (WriteErrorException) {
            throw;
        } catch (Exception ex) {
            throw new WriteErrorException($"{InstanceKey}: 写入节点失败。NodeId={nodeId.Value}：{ex.Message}", ex);
        }
    }

    public async Task<IReadOnlyList<OpcUaWriteResult>> WriteAsync(IReadOnlyList<OpcUaWriteItem> items, CancellationToken cancellationToken = default) {
        ThrowIfDisposed();
        Session session = GetActiveSession();

        if (items.Count == 0) {
            return [];
        }

        try {
            WriteValueCollection toWrite = [];
            foreach (OpcUaWriteItem item in items) {
                toWrite.Add(new WriteValue {
                    NodeId = OpcUaValueConverter.ToNodeId(item.NodeId),
                    AttributeId = Attributes.Value,
                    Value = new DataValue(item.Value is null ? Variant.Null : new Variant(item.Value))
                });
            }

            WriteResponse response = await session
                .WriteAsync(null, toWrite, cancellationToken)
                .ConfigureAwait(false);

            StatusCodeCollection results = response.Results;
            if (results is null || results.Count != items.Count) {
                throw new WriteErrorException(
                    $"{InstanceKey}: 批量写入返回结果数量不匹配。Expected={items.Count}, Actual={results?.Count ?? 0}。");
            }

            List<OpcUaWriteResult> writeResults = new(items.Count);
            for (int i = 0; i < items.Count; i++) {
                writeResults.Add(new OpcUaWriteResult(items[i].NodeId, results[i].Code));
            }

            return writeResults;
        } catch (OperationCanceledException) {
            throw;
        } catch (WriteErrorException) {
            throw;
        } catch (Exception ex) {
            throw new WriteErrorException($"{InstanceKey}: 批量写入失败。Count={items.Count}：{ex.Message}", ex);
        }
    }

    public async Task<IReadOnlyList<OpcUaBrowseNode>> BrowseAsync(OpcUaNodeId? parentNodeId = null, CancellationToken cancellationToken = default) {
        ThrowIfDisposed();
        Session session = GetActiveSession();

        try {
            NodeId startNode = parentNodeId.HasValue
                ? OpcUaValueConverter.ToNodeId(parentNodeId.Value)
                : ObjectIds.ObjectsFolder;

            BrowseDescriptionCollection nodesToBrowse = [
                new BrowseDescription {
                    NodeId = startNode,
                    BrowseDirection = BrowseDirection.Forward,
                    ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences,
                    IncludeSubtypes = true,
                    NodeClassMask = 0,
                    ResultMask = (uint)BrowseResultMask.All
                }
            ];

            BrowseResponse response = await session
                .BrowseAsync(null, null, 0, nodesToBrowse, cancellationToken)
                .ConfigureAwait(false);

            BrowseResult result = response.Results[0];
            if (OpcUaValueConverter.IsBad(result.StatusCode.Code)) {
                throw new OpcUaServiceException(
                    $"{InstanceKey}: 浏览失败。StatusCode=0x{result.StatusCode.Code:X8}。", result.StatusCode.Code);
            }

            List<OpcUaBrowseNode> nodes = [];
            foreach (ReferenceDescription reference in result.References) {
                NodeId? targetNode = ExpandedNodeId.ToNodeId(reference.NodeId, session.NamespaceUris);
                nodes.Add(new OpcUaBrowseNode(
                    new OpcUaNodeId(targetNode?.ToString() ?? reference.NodeId.ToString()),
                    reference.BrowseName?.Name ?? string.Empty,
                    reference.DisplayName?.Text ?? string.Empty,
                    OpcUaValueConverter.MapNodeClass(reference.NodeClass),
                    reference.IsForward));
            }

            return nodes;
        } catch (OperationCanceledException) {
            throw;
        } catch (OpcUaServiceException) {
            throw;
        } catch (Exception ex) {
            throw new OpcUaServiceException($"{InstanceKey}: 浏览失败：{ex.Message}", ex);
        }
    }

    public async Task<OpcUaMethodResult> CallMethodAsync(
        OpcUaNodeId objectNodeId,
        OpcUaNodeId methodNodeId,
        IReadOnlyList<object?> inputArguments,
        CancellationToken cancellationToken = default) {
        ThrowIfDisposed();
        Session session = GetActiveSession();

        try {
            VariantCollection arguments = [];
            foreach (object? argument in inputArguments) {
                arguments.Add(argument is null ? Variant.Null : new Variant(argument));
            }

            CallMethodRequestCollection requests = [
                new CallMethodRequest {
                    ObjectId = OpcUaValueConverter.ToNodeId(objectNodeId),
                    MethodId = OpcUaValueConverter.ToNodeId(methodNodeId),
                    InputArguments = arguments
                }
            ];

            CallResponse response = await session
                .CallAsync(null, requests, cancellationToken)
                .ConfigureAwait(false);

            CallMethodResult result = response.Results[0];

            List<object?> outputs = [];
            if (result.OutputArguments is not null) {
                foreach (Variant output in result.OutputArguments) {
                    outputs.Add(output.Value);
                }
            }

            return new OpcUaMethodResult(result.StatusCode.Code, outputs);
        } catch (OperationCanceledException) {
            throw;
        } catch (Exception ex) {
            throw new OpcUaServiceException(
                $"{InstanceKey}: 方法调用失败。Object={objectNodeId.Value}, Method={methodNodeId.Value}：{ex.Message}", ex);
        }
    }

    public async Task<IOpcUaSubscription> SubscribeAsync(
        IReadOnlyList<OpcUaMonitoredItem> items,
        OpcUaSubscriptionOptions options,
        CancellationToken cancellationToken = default) {
        ThrowIfDisposed();
        Session session = GetActiveSession();

        OpcUaSubscription subscription = new(options, items);
        try {
            await subscription.AttachAsync(session, cancellationToken).ConfigureAwait(false);
        } catch (OperationCanceledException) {
            subscription.Dispose();
            throw;
        } catch (Exception ex) {
            subscription.Dispose();
            throw new OpcUaServiceException($"{InstanceKey}: 创建订阅失败：{ex.Message}", ex);
        }

        subscription.Closed += OnSubscriptionClosed;
        lock (_subscriptions) {
            _subscriptions.Add(subscription);
        }

        return subscription;
    }

    #region 私有方法

    private async Task EnsureApplicationAsync() {
        if (_appConfig is not null) {
            return;
        }

        (_, _appConfig) = await OpcUaCertificateManager.CreateApplicationAsync(_config).ConfigureAwait(false);
    }

    private async Task EstablishSessionAsync(CancellationToken cts) {
        CloseSessionInternal();

        ConfiguredEndpoint endpoint = await OpcUaEndpointSelector
            .SelectAsync(_appConfig!, _config, cts).ConfigureAwait(false);
        IUserIdentity identity = await BuildIdentityAsync(cts).ConfigureAwait(false);

        Session session = await Session.Create(
            _appConfig!,
            endpoint,
            false,
            false,
            _config.ApplicationName,
            (uint)_config.SessionTimeout,
            identity,
            null,
            cts).ConfigureAwait(false);

        if (_config.KeepAliveInterval > 0) {
            session.KeepAliveInterval = _config.KeepAliveInterval;
        }

        session.KeepAlive += OnKeepAlive;
        _session = session;
        _keepAliveOk = true;

        List<OpcUaSubscription> subscriptions;
        lock (_subscriptions) {
            subscriptions = [.. _subscriptions];
        }

        foreach (OpcUaSubscription subscription in subscriptions) {
            await subscription.AttachAsync(session, cts).ConfigureAwait(false);
        }
    }

    private async Task<IUserIdentity> BuildIdentityAsync(CancellationToken cts) {
        switch (_config.IdentityType) {
            case OpcUaIdentityType.UserName:
                return await BuildUserNameIdentityAsync(cts).ConfigureAwait(false);

            case OpcUaIdentityType.Certificate: {
                var certificate = await OpcUaCertificateManager
                    .LoadClientCertificateAsync(_appConfig!, _config).ConfigureAwait(false);
                return new UserIdentity(certificate);
            }

            case OpcUaIdentityType.Anonymous:
            default:
                return new UserIdentity();
        }
    }

    private async Task<IUserIdentity> BuildUserNameIdentityAsync(CancellationToken cts) {
        string user = _config.UserName ?? string.Empty;
        string password = string.Empty;

        bool canResolveCredential = _config.CredentialProvider is not null && !string.IsNullOrEmpty(_config.CredentialKey);
        if (canResolveCredential) {
            OpcUaUserCredential? credential = await _config.CredentialProvider!
                .GetCredentialAsync(_config.CredentialKey!, cts).ConfigureAwait(false);

            if (credential is not null) {
                user = string.IsNullOrEmpty(credential.UserName) ? user : credential.UserName;
                password = credential.Password ?? string.Empty;
            }
        }

        return new UserIdentity(user, Encoding.UTF8.GetBytes(password));
    }

    private Session GetActiveSession() {
        Session? session = _session;
        if (session is null || !session.Connected) {
            throw new ConnectionException($"{InstanceKey}: 会话未建立或已断开。");
        }

        return session;
    }

    private void CloseSessionInternal() {
        Session? session = _session;
        _session = null;
        _keepAliveOk = false;

        if (session is null) {
            return;
        }

        try {
            session.KeepAlive -= OnKeepAlive;
        } catch {
            // 忽略事件解绑异常
        }

        try {
            _ = session.Close();
        } catch {
            // 忽略关闭异常
        }

        try {
            session.Dispose();
        } catch {
            // 忽略释放异常
        }
    }

    private void OnKeepAlive(ISession session, KeepAliveEventArgs e) {
        if (_disposed || !ReferenceEquals(session, _session)) {
            return;
        }

        if (ServiceResult.IsBad(e.Status)) {
            _keepAliveOk = false;
            BeginAutoReconnect();
        } else {
            _keepAliveOk = true;
        }
    }

    private void BeginAutoReconnect() {
        lock (_reconnectLock) {
            if (_disposed || _reconnectHandler is not null || _session is null) {
                return;
            }

            _reconnectHandler = new SessionReconnectHandler(true);
            int period = _config.ReconnectDelay > 0 ? _config.ReconnectDelay : 1000;
            _ = _reconnectHandler.BeginReconnect(_session, period, OnReconnectComplete);
        }
    }

    private void OnReconnectComplete(object? sender, EventArgs e) {
        lock (_reconnectLock) {
            if (!ReferenceEquals(sender, _reconnectHandler) || _reconnectHandler is null) {
                return;
            }

            if (_reconnectHandler.Session is Session newSession && !ReferenceEquals(newSession, _session)) {
                Session? old = _session;
                if (old is not null) {
                    try {
                        old.KeepAlive -= OnKeepAlive;
                    } catch {
                        // 忽略事件解绑异常
                    }
                }

                if (_config.KeepAliveInterval > 0) {
                    newSession.KeepAliveInterval = _config.KeepAliveInterval;
                }

                newSession.KeepAlive += OnKeepAlive;
                _session = newSession;
            }

            _reconnectHandler.Dispose();
            _reconnectHandler = null;
            _keepAliveOk = true;
        }
    }

    private void StopReconnect() {
        lock (_reconnectLock) {
            if (_reconnectHandler is null) {
                return;
            }

            try {
                _reconnectHandler.CancelReconnect();
            } catch {
                // 忽略取消重连异常
            }

            try {
                _reconnectHandler.Dispose();
            } catch {
                // 忽略释放异常
            }

            _reconnectHandler = null;
        }
    }

    private void OnSubscriptionClosed(object? sender, EventArgs e) {
        if (sender is not OpcUaSubscription subscription) {
            return;
        }

        subscription.Closed -= OnSubscriptionClosed;
        lock (_subscriptions) {
            _ = _subscriptions.Remove(subscription);
        }
    }

    private void ThrowIfDisposed() {
#if !NETFRAMEWORK
        ObjectDisposedException.ThrowIf(_disposed, this);
#else
        if (_disposed) {
            throw new ObjectDisposedException(GetType().FullName);
        }
#endif
    }

    #endregion
}

// =============================================================================
// MAS.Communication
// https://www.mas-automation.com/
//
// Copyright 2026 MAS (厦门威光) Corporation
//
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.
// =============================================================================

using System.Net.WebSockets;
using System.Text;

namespace MAS.Communication.WebSocketProtocol;

internal sealed class WebSocketProtocolClient : IWebSocketProtocol {
    private const int DEFAULT_RECEIVE_BUFFER_SIZE = 16 * 1024;
    private const int DEFAULT_MAX_MESSAGE_SIZE = 16 * 1024 * 1024;
    private const int DEFAULT_CLOSE_TIMEOUT = 3000;
    private const int DEFAULT_RECONNECT_DELAY = 1000;

    private readonly IWebSocketCommunicationConfig _config;
    private readonly Func<IWebSocketConnection> _connectionFactory;
    private readonly SemaphoreSlim _connectLock = new(1, 1);
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly object _reconnectLock = new();

    private IWebSocketConnection? _connection;
    private Task? _receiveTask;
    private CancellationTokenSource? _receiveCts;
    private CancellationTokenSource? _reconnectCts;
    private volatile bool _userClosed = true;
    private volatile bool _disposed;

    public WebSocketProtocolClient(IWebSocketCommunicationConfig config)
        : this(config, null) {
    }

    /// <summary>
    /// 供单元测试注入可控连接实现的内部构造函数
    /// </summary>
    internal WebSocketProtocolClient(IWebSocketCommunicationConfig config, Func<IWebSocketConnection>? connectionFactory) {
        _config = config.Clone<IWebSocketCommunicationConfig>();
        _connectionFactory = connectionFactory ?? (() => new ClientWebSocketConnection(_config));
    }

    public event EventHandler? Disposed;
    public event EventHandler<WebSocketMessageReceivedEventArgs>? MessageReceived;
    public event EventHandler<WebSocketClosedEventArgs>? Closed;
    public event EventHandler? Reconnected;

    public CommProtocol ProtocolType => CommProtocol.WebSocket;

    public ICommunicationConfig Configuration => _config;

    public WebSocketState State => _connection?.State ?? WebSocketState.None;

    private string InstanceKey => _config.GetInstanceKey();

    private int ReceiveBufferBytes => _config.ReceiveBufferSize > 0 ? _config.ReceiveBufferSize : DEFAULT_RECEIVE_BUFFER_SIZE;

    private int MaxMessageBytes => _config.MaxMessageSize > 0 ? _config.MaxMessageSize : DEFAULT_MAX_MESSAGE_SIZE;

    private int CloseTimeoutMs => _config.CloseTimeout > 0 ? _config.CloseTimeout : DEFAULT_CLOSE_TIMEOUT;

    public async Task ConnectAsync(CancellationToken cts = default) {
        ThrowIfDisposed();
        StopAutoReconnect();

        await _connectLock.WaitAsync(cts).ConfigureAwait(false);
        try {
            _userClosed = false;
            await EstablishAsync(cts).ConfigureAwait(false);
        } catch (OperationCanceledException) {
            throw;
        } catch (ObjectDisposedException) {
            throw;
        } catch (ConnectionException) {
            throw;
        } catch (Exception ex) {
            throw new ConnectionException($"{InstanceKey}: WebSocket 连接失败：{ex.Message}", ex);
        } finally {
            _ = _connectLock.Release();
        }
    }

    public async Task<bool> ProbeConnectionAsync(CancellationToken cts = default) {
        ThrowIfDisposed();

        IWebSocketConnection? probe = null;
        try {
            Uri endpoint = BuildEndpointUri();
            probe = _connectionFactory();
            await ConnectWithTimeoutAsync(probe, endpoint, cts).ConfigureAwait(false);
            await SendCloseFrameAsync(probe, WebSocketCloseStatus.NormalClosure, null).ConfigureAwait(false);
            return true;
        } catch (OperationCanceledException) {
            throw;
        } catch {
            return false;
        } finally {
            if (probe is not null) {
                DisposeConnectionQuietly(probe);
            }
        }
    }

    public bool CheckConnection() {
        return _connection is { State: WebSocketState.Open };
    }

    public async Task<bool> TryReconnectAsync(int maxAttempts, int retryDelay = 1000, CancellationToken cts = default) {
        ThrowIfDisposed();
        StopAutoReconnect();

        await _connectLock.WaitAsync(cts).ConfigureAwait(false);
        try {
            _userClosed = false;
            return await ReconnectCoreAsync(maxAttempts, retryDelay, cts).ConfigureAwait(false);
        } finally {
            _ = _connectLock.Release();
        }
    }

    public Task SendTextAsync(string message, CancellationToken token = default) {
#if !NETFRAMEWORK
        ArgumentNullException.ThrowIfNull(message);
#else
        if (message is null) {
            throw new ArgumentNullException(nameof(message));
        }
#endif

        return SendCoreAsync(Encoding.UTF8.GetBytes(message), WebSocketMessageType.Text, token);
    }

    public Task SendBinaryAsync(byte[] message, CancellationToken token = default) {
#if !NETFRAMEWORK
        ArgumentNullException.ThrowIfNull(message);
#else
        if (message is null) {
            throw new ArgumentNullException(nameof(message));
        }
#endif

        return SendCoreAsync(message, WebSocketMessageType.Binary, token);
    }

    public async Task DisconnectAsync(CancellationToken token = default) {
        ThrowIfDisposed();
        _userClosed = true;
        StopAutoReconnect();

        await _connectLock.WaitAsync(token).ConfigureAwait(false);
        try {
            await CloseGracefullyAsync().ConfigureAwait(false);
        } finally {
            _ = _connectLock.Release();
        }
    }

    public void Disconnect() {
        _userClosed = true;
        StopAutoReconnect();

        _connectLock.Wait();
        try {
            CloseGracefullyAsync().GetAwaiter().GetResult();
        } finally {
            _ = _connectLock.Release();
        }
    }

    public void Dispose() {
        if (_disposed) {
            return;
        }

        _disposed = true;
        _userClosed = true;

        StopAutoReconnect();
        _ = DetachConnection();

        Disposed?.Invoke(this, EventArgs.Empty);
    }

    #region 连接建立

    private async Task EstablishAsync(CancellationToken cts) {
        PublishDetachedClose(DetachConnection(), null);

        Uri endpoint = BuildEndpointUri();
        IWebSocketConnection connection = _connectionFactory();

        try {
            await ConnectWithTimeoutAsync(connection, endpoint, cts).ConfigureAwait(false);
        } catch {
            DisposeConnectionQuietly(connection);
            throw;
        }

        _connection = connection;
        StartReceiveLoop(connection);

        if (_disposed) {
            _ = DetachConnection();
            ThrowIfDisposed();
        }
    }

    private async Task ConnectWithTimeoutAsync(IWebSocketConnection connection, Uri endpoint, CancellationToken cts) {
        using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts);
        if (_config.ConnectTimeout > 0) {
            linkedCts.CancelAfter(_config.ConnectTimeout);
        }

        try {
            await connection.ConnectAsync(endpoint, linkedCts.Token).ConfigureAwait(false);
        } catch (OperationCanceledException) when (!cts.IsCancellationRequested) {
            throw new ConnectionException($"{InstanceKey}: 连接超时（{_config.ConnectTimeout}ms）：{endpoint}");
        }
    }

    private Uri BuildEndpointUri() {
        string url = _config.EndpointUrl?.Trim() ?? string.Empty;

        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? endpoint)) {
            throw new ConnectionException($"{InstanceKey}: EndpointUrl 不是有效的绝对地址。");
        }

        bool isWebSocketScheme =
            string.Equals(endpoint.Scheme, "ws", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(endpoint.Scheme, "wss", StringComparison.OrdinalIgnoreCase);

        if (!isWebSocketScheme) {
            throw new ConnectionException(
                $"{InstanceKey}: EndpointUrl 仅支持 ws:// 或 wss://，不接受 {endpoint.Scheme}://。");
        }

        return endpoint;
    }

    private async Task<bool> ReconnectCoreAsync(int maxAttempts, int retryDelay, CancellationToken cts) {
        for (int attempt = 0; attempt < maxAttempts; attempt++) {
            cts.ThrowIfCancellationRequested();

            try {
                await EstablishAsync(cts).ConfigureAwait(false);
                return true;
            } catch (OperationCanceledException) {
                throw;
            } catch {
                // 单次尝试失败，等待后重试
            }

            await Task.Delay(retryDelay, cts).ConfigureAwait(false);
        }

        return false;
    }

    #endregion

    #region 消息发送

    private async Task SendCoreAsync(byte[] payload, WebSocketMessageType messageType, CancellationToken token) {
        ThrowIfDisposed();

        if (payload.Length > MaxMessageBytes) {
            throw new WriteErrorException(
                $"{InstanceKey}: 消息大小 {payload.Length} 字节超过上限 {MaxMessageBytes} 字节，已拒绝发送。");
        }

        IWebSocketConnection connection = GetOpenConnection();

        await _sendLock.WaitAsync(token).ConfigureAwait(false);
        try {
            await SendFrameAsync(connection, payload, messageType, token).ConfigureAwait(false);
        } finally {
            _ = _sendLock.Release();
        }
    }

    private async Task SendFrameAsync(IWebSocketConnection connection, byte[] payload, WebSocketMessageType messageType, CancellationToken token) {
        using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token);
        if (_config.WriteTimeout > 0) {
            linkedCts.CancelAfter(_config.WriteTimeout);
        }

        try {
            await connection.SendAsync(payload, messageType, linkedCts.Token).ConfigureAwait(false);
        } catch (OperationCanceledException) when (!token.IsCancellationRequested) {
            throw new WriteErrorException($"{InstanceKey}: 发送超时（{_config.WriteTimeout}ms）。");
        } catch (OperationCanceledException) {
            throw;
        } catch (Exception ex) {
            throw new WriteErrorException($"{InstanceKey}: 发送失败：{ex.Message}", ex);
        }
    }

    private IWebSocketConnection GetOpenConnection() {
        IWebSocketConnection? connection = _connection;
        if (connection is null || connection.State != WebSocketState.Open) {
            throw new ConnectionException($"{InstanceKey}: 连接未建立或已断开。");
        }

        return connection;
    }

    #endregion

    #region 后台接收

    private void StartReceiveLoop(IWebSocketConnection connection) {
        CancellationTokenSource cts = new();
        _receiveCts = cts;
        byte[] buffer = new byte[ReceiveBufferBytes];
        _receiveTask = Task.Run(() => ReceiveLoopAsync(connection, buffer, cts.Token));
    }

    private async Task ReceiveLoopAsync(IWebSocketConnection connection, byte[] buffer, CancellationToken token) {
        WebSocketCloseStatus? closeStatus = null;
        string? closeDescription = null;
        Exception? error = null;

        try {
            bool hasMore = true;
            while (hasMore) {
                hasMore = await ReceiveAndDispatchAsync(connection, buffer, token).ConfigureAwait(false);
            }

            closeStatus = connection.CloseStatus ?? WebSocketCloseStatus.NormalClosure;
            closeDescription = connection.CloseStatusDescription;
        } catch (OperationCanceledException) {
            // 本地主动摘除连接（关闭、重建或释放），Closed 事件由摘除方负责发布
            return;
        } catch (Exception ex) {
            error = ex;
        }

        OnReceiveLoopEnded(connection, closeStatus, closeDescription, error);
    }

    private async Task<bool> ReceiveAndDispatchAsync(IWebSocketConnection connection, byte[] buffer, CancellationToken token) {
        using MemoryStream message = new();
        WebSocketFrame frame;

        do {
            frame = await connection.ReceiveAsync(buffer, token).ConfigureAwait(false);
            if (frame.MessageType == WebSocketMessageType.Close) {
                await AcknowledgeRemoteCloseAsync(connection).ConfigureAwait(false);
                return false;
            }

            message.Write(buffer, 0, frame.Count);
            await EnsureWithinLimitAsync(connection, message.Length).ConfigureAwait(false);
        } while (!frame.EndOfMessage);

        RaiseMessageReceived(new WebSocketMessageReceivedEventArgs(frame.MessageType, message.ToArray()));
        return true;
    }

    private async Task EnsureWithinLimitAsync(IWebSocketConnection connection, long receivedBytes) {
        if (receivedBytes <= MaxMessageBytes) {
            return;
        }

        await SendCloseFrameAsync(connection, WebSocketCloseStatus.MessageTooBig, "message too big").ConfigureAwait(false);
        throw new ConnectionException($"{InstanceKey}: 接收消息超过大小上限（{MaxMessageBytes} 字节），连接已关闭。");
    }

    private async Task AcknowledgeRemoteCloseAsync(IWebSocketConnection connection) {
        if (connection.State == WebSocketState.CloseReceived) {
            await SendCloseFrameAsync(connection, WebSocketCloseStatus.NormalClosure, null).ConfigureAwait(false);
        }
    }

    private void OnReceiveLoopEnded(IWebSocketConnection connection, WebSocketCloseStatus? closeStatus, string? closeDescription, Exception? error) {
        if (_disposed) {
            return;
        }

        // 原子摘除是 Closed 事件的唯一发布许可；摘除失败说明关闭/重建流程已接管该连接
        if (!ReferenceEquals(Interlocked.CompareExchange(ref _connection, null, connection), connection)) {
            return;
        }

        DisposeConnectionQuietly(connection);
        RaiseClosed(new WebSocketClosedEventArgs(closeStatus, closeDescription, error));

        bool canAutoReconnect = error is not null && !_userClosed && _config.AutoReconnect;
        if (canAutoReconnect) {
            StartAutoReconnect();
        }
    }

    #endregion

    #region 自动重连

    private void StartAutoReconnect() {
        lock (_reconnectLock) {
            if (_disposed || _reconnectCts is not null) {
                return;
            }

            CancellationTokenSource cts = new();
            _reconnectCts = cts;
            _ = Task.Run(() => AutoReconnectAsync(cts));
        }
    }

    private async Task AutoReconnectAsync(CancellationTokenSource ownCts) {
        bool isReconnected = false;
        try {
            isReconnected = await RunAutoReconnectAttemptsAsync(ownCts.Token).ConfigureAwait(false);
        } catch (OperationCanceledException) {
            // 自动重连被取消（主动断开或释放）
        } catch {
            // 自动重连过程异常，放弃本轮重连
        } finally {
            FinishAutoReconnect(ownCts);
        }

        if (isReconnected) {
            RaiseReconnected();
        }
    }

    private async Task<bool> RunAutoReconnectAttemptsAsync(CancellationToken token) {
        int maxAttempts = Math.Max(1, _config.MaxRetries);
        int delay = _config.ReconnectDelay > 0 ? _config.ReconnectDelay : DEFAULT_RECONNECT_DELAY;

        for (int attempt = 0; attempt < maxAttempts; attempt++) {
            await Task.Delay(delay, token).ConfigureAwait(false);

            bool isConnected = await TryEstablishOnceAsync(token).ConfigureAwait(false);
            if (isConnected) {
                return true;
            }
        }

        return false;
    }

    private async Task<bool> TryEstablishOnceAsync(CancellationToken token) {
        await _connectLock.WaitAsync(token).ConfigureAwait(false);
        try {
            if (_userClosed || _disposed) {
                throw new OperationCanceledException("连接状态已由调用方接管，结束自动重连。");
            }

            await EstablishAsync(token).ConfigureAwait(false);
            return true;
        } catch (OperationCanceledException) {
            throw;
        } catch {
            return false;
        } finally {
            _ = _connectLock.Release();
        }
    }

    private void StopAutoReconnect() {
        lock (_reconnectLock) {
            if (_reconnectCts is null) {
                return;
            }

            try {
                _reconnectCts.Cancel();
            } catch {
                // 重连任务可能已自行结束并释放，忽略
            }
        }
    }

    private void FinishAutoReconnect(CancellationTokenSource ownCts) {
        lock (_reconnectLock) {
            if (ReferenceEquals(_reconnectCts, ownCts)) {
                _reconnectCts = null;
            }
        }

        ownCts.Dispose();
    }

    #endregion

    #region 关闭与释放

    private async Task CloseGracefullyAsync() {
        IWebSocketConnection? connection = _connection;
        Task? receiveTask = _receiveTask;

        bool canHandshake = connection is not null &&
            (connection.State == WebSocketState.Open || connection.State == WebSocketState.CloseReceived);

        if (canHandshake) {
            // 只发送关闭帧，由接收循环消费对端的关闭确认并发布 Closed 事件
            await SendCloseFrameAsync(connection!, WebSocketCloseStatus.NormalClosure, null).ConfigureAwait(false);
            await WaitReceiveLoopEndAsync(receiveTask).ConfigureAwait(false);
        }

        // 对端未确认关闭帧（握手超时）时接收循环无法发布 Closed，由关闭流程统一补发
        PublishDetachedClose(DetachConnection(), null);
    }

    private void PublishDetachedClose(IWebSocketConnection? connection, Exception? error) {
        if (connection is null || _disposed) {
            return;
        }

        RaiseClosed(new WebSocketClosedEventArgs(
            connection.CloseStatus ?? WebSocketCloseStatus.NormalClosure,
            connection.CloseStatusDescription,
            error));
    }

    private async Task SendCloseFrameAsync(IWebSocketConnection connection, WebSocketCloseStatus status, string? description) {
        try {
            using CancellationTokenSource closeCts = new();
            closeCts.CancelAfter(CloseTimeoutMs);
            await connection.CloseOutputAsync(status, description, closeCts.Token).ConfigureAwait(false);
        } catch {
            // 关闭帧发送失败时直接进入中止流程
        }
    }

    private async Task WaitReceiveLoopEndAsync(Task? receiveTask) {
        if (receiveTask is null) {
            return;
        }

        // 接收循环自身不抛异常；超时未结束则由后续的连接摘除中止
        _ = await Task.WhenAny(receiveTask, Task.Delay(CloseTimeoutMs)).ConfigureAwait(false);
    }

    private IWebSocketConnection? DetachConnection() {
        CancellationTokenSource? cts = _receiveCts;
        _receiveCts = null;
        _receiveTask = null;

        if (cts is not null) {
            try {
                cts.Cancel();
            } catch {
                // 忽略取消异常
            }

            cts.Dispose();
        }

        IWebSocketConnection? connection = Interlocked.Exchange(ref _connection, null);
        if (connection is not null) {
            DisposeConnectionQuietly(connection);
        }

        return connection;
    }

    private static void DisposeConnectionQuietly(IWebSocketConnection connection) {
        try {
            connection.Abort();
        } catch {
            // 忽略中止异常
        }

        try {
            connection.Dispose();
        } catch {
            // 忽略释放异常
        }
    }

    #endregion

    #region 事件派发

    private void RaiseMessageReceived(WebSocketMessageReceivedEventArgs e) {
        try {
            MessageReceived?.Invoke(this, e);
        } catch {
            // 调用方事件处理器异常不允许中断接收循环
        }
    }

    private void RaiseClosed(WebSocketClosedEventArgs e) {
        try {
            Closed?.Invoke(this, e);
        } catch {
            // 调用方事件处理器异常不允许影响内部状态
        }
    }

    private void RaiseReconnected() {
        try {
            Reconnected?.Invoke(this, EventArgs.Empty);
        } catch {
            // 调用方事件处理器异常不允许影响内部状态
        }
    }

    #endregion

    private void ThrowIfDisposed() {
#if !NETFRAMEWORK
        ObjectDisposedException.ThrowIf(_disposed, this);
#else
        if (_disposed) {
            throw new ObjectDisposedException(GetType().FullName);
        }
#endif
    }
}

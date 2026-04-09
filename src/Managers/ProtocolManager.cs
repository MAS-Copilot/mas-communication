// =============================================================================
// MAS.Communication
// https://www.mas-automation.com/
//
// Copyright 2026 MAS (厦门威光) Corporation
//
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.
// =============================================================================

using MAS.Communication.McProtocol;
using MAS.Communication.ModbusProtocol;
using MAS.Communication.S7Protocol;
using System.Collections.Concurrent;

namespace MAS.Communication;

internal sealed class ProtocolManager : IProtocolManager {
    private readonly ConcurrentDictionary<string, Lazy<IProtocol>> _protocols = new(StringComparer.Ordinal);
    private bool _disposed;

    public int Count => _protocols.Count;

    public event EventHandler<ProtocolCreatedEventArgs>? ProtocolCreated;
    public event EventHandler<ProtocolRemovedEventArgs>? ProtocolRemoved;

    public bool Contains(string instanceId) {
        return _protocols.ContainsKey(instanceId);
    }

    public bool Contains(IProtocol protocol) {
        string instanceId = protocol.Configuration.GetInstanceKey();
        if (!_protocols.TryGetValue(instanceId, out Lazy<IProtocol>? lazyProtocol)) {
            return false;
        }

        return lazyProtocol.IsValueCreated &&
               ReferenceEquals(lazyProtocol.Value, protocol);
    }

    public IProtocol GetOrCreate(ICommunicationConfig config) {
#if !NETFRAMEWORK
        ObjectDisposedException.ThrowIf(_disposed, this);
#else
        if (_disposed) {
            throw new ObjectDisposedException(GetType().FullName);
        }
#endif

        string instanceId = config.GetInstanceKey();
        Lazy<IProtocol> newLazyProtocol = new(
            () => CreateProtocol(config),
            LazyThreadSafetyMode.ExecutionAndPublication);

        Lazy<IProtocol> lazyProtocol = _protocols.GetOrAdd(instanceId, newLazyProtocol);
        IProtocol protocol = lazyProtocol.Value;

        if (ReferenceEquals(lazyProtocol, newLazyProtocol)) {
            protocol.Disposed += OnProtocolDisposed;
            OnProtocolCreated(protocol);
        }

        return protocol;
    }

    public TProtocol GetOrCreate<TProtocol>(ICommunicationConfig config) where TProtocol : class, IProtocol {
        IProtocol protocol = GetOrCreate(config);

        if (protocol is not TProtocol typedProtocol) {
            throw new InvalidCastException(
                $"协议实例类型不匹配，实际类型为 {protocol.GetType().FullName}，目标类型为 {typeof(TProtocol).FullName}。");
        }

        return typedProtocol;
    }

    public IProtocol? Get(string instanceId) {
        if (!_protocols.TryGetValue(instanceId, out var lazyProtocol)) {
            return null;
        }

        return lazyProtocol.IsValueCreated ? lazyProtocol.Value : null;
    }

    public TProtocol? Get<TProtocol>(string instanceId) where TProtocol : class, IProtocol {
        return Get(instanceId) as TProtocol;
    }

    public IReadOnlyCollection<string> GetInstanceIds() {
        return [.. _protocols.Keys];
    }

    public bool Remove(string instanceId) {
#if !NETFRAMEWORK
        ObjectDisposedException.ThrowIf(_disposed, this);
#else
        if (_disposed) {
            throw new ObjectDisposedException(GetType().FullName);
        }
#endif

        if (!_protocols.TryRemove(instanceId, out Lazy<IProtocol>? lazyProtocol)) {
            return false;
        }

        OnProtocolRemoved(instanceId);
        DisposeProtocol(lazyProtocol);
        return true;
    }

    public bool Remove(IProtocol protocol) {
#if !NETFRAMEWORK
        ObjectDisposedException.ThrowIf(_disposed, this);
#else
        if (_disposed) {
            throw new ObjectDisposedException(GetType().FullName);
        }
#endif

        string instanceId = protocol.Configuration.GetInstanceKey();

        if (!_protocols.TryGetValue(instanceId, out Lazy<IProtocol>? lazyProtocol) ||
            !lazyProtocol.IsValueCreated ||
            !ReferenceEquals(lazyProtocol.Value, protocol)) {
            return false;
        }

        return Remove(instanceId);
    }

    public void Dispose() {
        if (_disposed) {
            return;
        }

        foreach (KeyValuePair<string, Lazy<IProtocol>> item in _protocols.ToArray()) {
            if (_protocols.TryRemove(item.Key, out Lazy<IProtocol>? lazyProtocol)) {
                OnProtocolRemoved(item.Key);
                DisposeProtocol(lazyProtocol);
            }
        }

        _disposed = true;
    }

    #region 私有方法

    private static IProtocol CreateProtocol(ICommunicationConfig config) {
        return config switch {
            IMcCommunicationConfig mc => new McProtocolClient(mc),
            IModbusCommunicationConfig md => new ModbusProtocolTcp(md),
            IS7CommunicationConfig s7 => new S7ProtocolClient(s7),
            _ => throw new NotSupportedException($"不支持的通讯配置类型：{config.GetInstanceKey()}")
        };
    }

    private void DisposeProtocol(Lazy<IProtocol>? lazyProtocol) {
        if (lazyProtocol is null || !lazyProtocol.IsValueCreated) {
            return;
        }

        IProtocol protocol = lazyProtocol.Value;
        protocol.Disposed -= OnProtocolDisposed;
        protocol.Dispose();
    }

    private void OnProtocolCreated(IProtocol protocol) {
        ProtocolCreated?.Invoke(this, new ProtocolCreatedEventArgs(protocol));
    }

    private void OnProtocolRemoved(string instanceId) {
        ProtocolRemoved?.Invoke(this, new ProtocolRemovedEventArgs(instanceId));
    }

    private void OnProtocolDisposed(object? sender, EventArgs e) {
        if (sender is not IProtocol protocol) {
            return;
        }

        string instanceId = protocol.Configuration.GetInstanceKey();

        if (_protocols.TryGetValue(instanceId, out Lazy<IProtocol>? lazyProtocol) &&
            lazyProtocol.IsValueCreated &&
            ReferenceEquals(lazyProtocol.Value, protocol)) {
            _ = _protocols.TryRemove(instanceId, out _);
            OnProtocolRemoved(instanceId);
        }
    }

    #endregion
}

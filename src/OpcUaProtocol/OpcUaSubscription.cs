// =============================================================================
// MAS.Communication
// https://www.mas-automation.com/
//
// Copyright 2026 MAS (厦门威光) Corporation
//
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.
// =============================================================================

using System.Collections.Concurrent;
using Opc.Ua;
using Opc.Ua.Client;

// 使用官方客户端的稳定同步友好重载；对应的遥测（ITelemetryContext）新 API 暂不在首版范围内
#pragma warning disable CS0618

namespace MAS.Communication.OpcUaProtocol;

/// <summary>
/// <see cref="IOpcUaSubscription"/> 的内部实现
/// </summary>
/// <remarks>
/// SDK 的发布回调线程只负责把数据变化入队，实际的 <see cref="DataChanged"/> 事件由内部的
/// 派发线程触发，避免用户业务处理阻塞 OPC UA 的 Publish 流程
/// </remarks>
internal sealed class OpcUaSubscription : IOpcUaSubscription {
    private readonly OpcUaSubscriptionOptions _options;
    private readonly Dictionary<string, OpcUaMonitoredItem> _specs = new(StringComparer.Ordinal);
    private readonly BlockingCollection<OpcUaDataChangedEventArgs> _queue = [];
    private readonly Task _dispatchTask;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private Subscription? _subscription;
    private bool _disposed;

    public string Id { get; }

    public event EventHandler<OpcUaDataChangedEventArgs>? DataChanged;

    /// <summary>
    /// 当订阅被删除或释放时触发，供客户端从跟踪列表中移除该订阅
    /// </summary>
    internal event EventHandler? Closed;

    public OpcUaSubscription(OpcUaSubscriptionOptions options, IReadOnlyList<OpcUaMonitoredItem> items) {
        _options = options;
        Id = "opcua-sub-" + Guid.NewGuid().ToString("N");

        foreach (OpcUaMonitoredItem item in items) {
            _specs[item.NodeId.Value] = item;
        }

        _dispatchTask = Task.Factory.StartNew(
            DispatchLoop,
            CancellationToken.None,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
    }

    /// <summary>
    /// 在指定会话上创建（或在重连后重建）服务端订阅及其监控项
    /// </summary>
    internal async Task AttachAsync(Session session, CancellationToken cancellationToken) {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try {
            Subscription subscription = new(session.DefaultSubscription) {
                DisplayName = Id,
                PublishingInterval = (int)_options.PublishingInterval,
                KeepAliveCount = _options.KeepAliveCount,
                LifetimeCount = _options.LifetimeCount,
                MaxNotificationsPerPublish = _options.MaxNotificationsPerPublish,
                PublishingEnabled = _options.PublishingEnabled
            };

            _ = session.AddSubscription(subscription);
            await subscription.CreateAsync(cancellationToken).ConfigureAwait(false);

            foreach (OpcUaMonitoredItem spec in _specs.Values) {
                subscription.AddItem(CreateMonitoredItem(spec));
            }

            await subscription.ApplyChangesAsync(cancellationToken).ConfigureAwait(false);
            _subscription = subscription;
        } finally {
            _ = _lock.Release();
        }
    }

    public async Task AddItemsAsync(IReadOnlyList<OpcUaMonitoredItem> items, CancellationToken cancellationToken = default) {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try {
            EnsureUsable();

            foreach (OpcUaMonitoredItem spec in items) {
                _specs[spec.NodeId.Value] = spec;
                _subscription!.AddItem(CreateMonitoredItem(spec));
            }

            await _subscription!.ApplyChangesAsync(cancellationToken).ConfigureAwait(false);
        } catch (OperationCanceledException) {
            throw;
        } catch (Exception ex) when (ex is not ObjectDisposedException) {
            throw new OpcUaServiceException($"{Id}: 添加监控项失败：{ex.Message}", ex);
        } finally {
            _ = _lock.Release();
        }
    }

    public async Task RemoveItemsAsync(IReadOnlyList<OpcUaNodeId> nodeIds, CancellationToken cancellationToken = default) {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try {
            EnsureUsable();

            HashSet<string> targets = [.. nodeIds.Select(n => n.Value)];
            List<MonitoredItem> toRemove = [.. _subscription!.MonitoredItems.Where(mi => targets.Contains(mi.DisplayName))];

            foreach (MonitoredItem item in toRemove) {
                item.Notification -= OnNotification;
                _subscription.RemoveItem(item);
            }

            foreach (OpcUaNodeId nodeId in nodeIds) {
                _ = _specs.Remove(nodeId.Value);
            }

            await _subscription.ApplyChangesAsync(cancellationToken).ConfigureAwait(false);
        } catch (OperationCanceledException) {
            throw;
        } catch (Exception ex) when (ex is not ObjectDisposedException) {
            throw new OpcUaServiceException($"{Id}: 移除监控项失败：{ex.Message}", ex);
        } finally {
            _ = _lock.Release();
        }
    }

    public async Task DeleteAsync(CancellationToken cancellationToken = default) {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try {
            if (_disposed) {
                return;
            }

            Subscription? subscription = _subscription;
            ISession? session = subscription?.Session;
            if (subscription is not null && session is not null) {
                _ = await session.RemoveSubscriptionAsync(subscription, cancellationToken).ConfigureAwait(false);
            }
        } catch (OperationCanceledException) {
            throw;
        } catch (Exception ex) {
            throw new OpcUaServiceException($"{Id}: 删除订阅失败：{ex.Message}", ex);
        } finally {
            _ = _lock.Release();
        }

        Dispose();
    }

    public void Dispose() {
        if (_disposed) {
            return;
        }

        _disposed = true;

        try {
            Subscription? subscription = _subscription;
            subscription?.DeleteAsync(true).GetAwaiter().GetResult();
        } catch {
            // 释放阶段忽略删除失败
        }

        _queue.CompleteAdding();
        try {
            _ = _dispatchTask.Wait(TimeSpan.FromSeconds(2));
        } catch {
            // 忽略派发线程等待异常
        }

        _queue.Dispose();
        _lock.Dispose();

        Closed?.Invoke(this, EventArgs.Empty);
    }

    private MonitoredItem CreateMonitoredItem(OpcUaMonitoredItem spec) {
        MonitoredItem item = new() {
            StartNodeId = OpcUaValueConverter.ToNodeId(spec.NodeId),
            AttributeId = Attributes.Value,
            DisplayName = spec.NodeId.Value,
            SamplingInterval = (int)spec.SamplingInterval,
            QueueSize = spec.QueueSize,
            DiscardOldest = spec.DiscardOldest
        };

        item.Notification += OnNotification;
        return item;
    }

    private void OnNotification(MonitoredItem monitoredItem, MonitoredItemNotificationEventArgs e) {
        OpcUaNodeId nodeId = new(monitoredItem.DisplayName);

        foreach (DataValue value in monitoredItem.DequeueValues()) {
            OpcUaValue opcValue = OpcUaValueConverter.ToOpcUaValue(nodeId, value);
            if (!_queue.IsAddingCompleted) {
                try {
                    _queue.Add(new OpcUaDataChangedEventArgs(Id, opcValue));
                } catch (InvalidOperationException) {
                    // 队列已关闭，忽略
                }
            }
        }
    }

    private void DispatchLoop() {
        foreach (OpcUaDataChangedEventArgs args in _queue.GetConsumingEnumerable()) {
            try {
                DataChanged?.Invoke(this, args);
            } catch {
                // 用户事件处理异常不应影响派发线程
            }
        }
    }

    private void EnsureUsable() {
#if !NETFRAMEWORK
        ObjectDisposedException.ThrowIf(_disposed, this);
#else
        if (_disposed) {
            throw new ObjectDisposedException(GetType().FullName);
        }
#endif
        if (_subscription is null) {
            throw new OpcUaServiceException($"{Id}: 订阅尚未创建。");
        }
    }
}

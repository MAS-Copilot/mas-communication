// =============================================================================
// MAS.Communication
// https://www.mas-automation.com/
//
// Copyright 2026 MAS (厦门威光) Corporation
//
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.
// =============================================================================

namespace MAS.Communication.OpcUaProtocol;

/// <summary>
/// OPC UA 订阅
/// </summary>
/// <remarks>
/// 订阅是一个独立的、有服务端资源的抽象，除数据变化事件外，还支持动态添加/删除监控项
/// 以及显式释放服务端资源
/// </remarks>
public interface IOpcUaSubscription : IDisposable {
    /// <summary>
    /// 获取订阅标识
    /// </summary>
    string Id { get; }

    /// <summary>
    /// 数据变化事件
    /// </summary>
    /// <remarks>事件在内部派发线程上触发，而非 SDK 的发布回调线程，避免业务处理阻塞发布流程</remarks>
    event EventHandler<OpcUaDataChangedEventArgs>? DataChanged;

    /// <summary>
    /// 异步向订阅添加监控项
    /// </summary>
    /// <param name="items">要添加的监控项</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>表示一个异步操作任务</returns>
    /// <exception cref="OperationCanceledException">取消此操作时抛出此异常</exception>
    /// <exception cref="OpcUaServiceException">服务调用失败时抛出此异常</exception>
    Task AddItemsAsync(
        IReadOnlyList<OpcUaMonitoredItem> items,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步从订阅移除监控项
    /// </summary>
    /// <param name="nodeIds">要移除的节点标识</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>表示一个异步操作任务</returns>
    /// <exception cref="OperationCanceledException">取消此操作时抛出此异常</exception>
    /// <exception cref="OpcUaServiceException">服务调用失败时抛出此异常</exception>
    Task RemoveItemsAsync(
        IReadOnlyList<OpcUaNodeId> nodeIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步删除订阅并释放服务端资源
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>表示一个异步操作任务</returns>
    /// <exception cref="OperationCanceledException">取消此操作时抛出此异常</exception>
    /// <exception cref="OpcUaServiceException">服务调用失败时抛出此异常</exception>
    Task DeleteAsync(CancellationToken cancellationToken = default);
}

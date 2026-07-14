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
/// 与 OPC UA 服务端进行通信的客户端接口
/// </summary>
/// <remarks>
/// OPC UA 保持自己的“节点、会话、订阅、安全”模型，不抽象成地址读写接口。
/// 接口只暴露库自有的轻量模型，不泄漏官方 SDK 类型
/// </remarks>
public interface IOpcUaProtocol : IProtocol {
    /// <summary>
    /// 异步读取单个节点的值
    /// </summary>
    /// <param name="nodeId">节点标识</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>读取结果，包含值、状态码与时间戳</returns>
    /// <exception cref="OperationCanceledException">取消此操作时抛出此异常</exception>
    /// <exception cref="ReadErrorException">读取失败时抛出此异常</exception>
    Task<OpcUaValue> ReadAsync(
        OpcUaNodeId nodeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步读取单个节点的值并转换为指定类型
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="nodeId">节点标识</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>转换后的值；节点值为空时返回默认值</returns>
    /// <exception cref="OperationCanceledException">取消此操作时抛出此异常</exception>
    /// <exception cref="ReadErrorException">读取失败或类型转换失败时抛出此异常</exception>
    Task<T?> ReadAsync<T>(
        OpcUaNodeId nodeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步批量读取多个节点的值
    /// </summary>
    /// <remarks>批量读取允许部分成功、部分失败，逐项状态通过 <see cref="OpcUaValue.StatusCode"/> 返回</remarks>
    /// <param name="items">要读取的节点项</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>与请求顺序一致的读取结果集合</returns>
    /// <exception cref="OperationCanceledException">取消此操作时抛出此异常</exception>
    /// <exception cref="ReadErrorException">整体性请求失败时抛出此异常</exception>
    Task<IReadOnlyList<OpcUaValue>> ReadAsync(
        IReadOnlyList<OpcUaReadItem> items,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步写入单个节点的值
    /// </summary>
    /// <param name="nodeId">节点标识</param>
    /// <param name="value">要写入的值</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>表示一个异步操作任务</returns>
    /// <exception cref="OperationCanceledException">取消此操作时抛出此异常</exception>
    /// <exception cref="WriteErrorException">写入失败时抛出此异常</exception>
    Task WriteAsync(
        OpcUaNodeId nodeId,
        object? value,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步批量写入多个节点的值
    /// </summary>
    /// <remarks>批量写入允许部分成功、部分失败，逐项状态通过返回结果的状态码返回</remarks>
    /// <param name="items">要写入的节点项</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>与请求顺序一致的写入结果集合</returns>
    /// <exception cref="OperationCanceledException">取消此操作时抛出此异常</exception>
    /// <exception cref="WriteErrorException">整体性请求失败时抛出此异常</exception>
    Task<IReadOnlyList<OpcUaWriteResult>> WriteAsync(
        IReadOnlyList<OpcUaWriteItem> items,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步浏览节点的子节点
    /// </summary>
    /// <param name="parentNodeId">父节点标识；为 <see langword="null"/> 时从 Objects 文件夹开始浏览</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>子节点集合</returns>
    /// <exception cref="OperationCanceledException">取消此操作时抛出此异常</exception>
    /// <exception cref="OpcUaServiceException">服务调用失败时抛出此异常</exception>
    Task<IReadOnlyList<OpcUaBrowseNode>> BrowseAsync(
        OpcUaNodeId? parentNodeId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步调用方法
    /// </summary>
    /// <param name="objectNodeId">拥有该方法的对象节点标识</param>
    /// <param name="methodNodeId">方法节点标识</param>
    /// <param name="inputArguments">输入参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>方法调用结果，包含状态码与输出参数</returns>
    /// <exception cref="OperationCanceledException">取消此操作时抛出此异常</exception>
    /// <exception cref="OpcUaServiceException">服务调用失败时抛出此异常</exception>
    Task<OpcUaMethodResult> CallMethodAsync(
        OpcUaNodeId objectNodeId,
        OpcUaNodeId methodNodeId,
        IReadOnlyList<object?> inputArguments,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步创建数据变化订阅
    /// </summary>
    /// <param name="items">初始监控项</param>
    /// <param name="options">订阅参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>订阅实例，可用于动态增删监控项及释放服务端资源</returns>
    /// <exception cref="OperationCanceledException">取消此操作时抛出此异常</exception>
    /// <exception cref="OpcUaServiceException">服务调用失败时抛出此异常</exception>
    Task<IOpcUaSubscription> SubscribeAsync(
        IReadOnlyList<OpcUaMonitoredItem> items,
        OpcUaSubscriptionOptions options,
        CancellationToken cancellationToken = default);
}

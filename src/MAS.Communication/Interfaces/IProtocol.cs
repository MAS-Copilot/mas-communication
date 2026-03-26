// =============================================================================
// Professional Automation Equipment Manufacturer.
// Website:       https://www.mas-automation.com/
//
// Copyright (c) MAS(厦门威光) Corporation. All rights reserved.
// =============================================================================

namespace MAS.Communication;

/// <summary>
/// 所有通信协议的统一接口
/// </summary>
public interface IProtocol : IDisposable {
    /// <summary>
    /// 当实例被释放后触发
    /// </summary>
    event EventHandler? Disposed;

    /// <summary>
    /// 获取协议类型
    /// </summary>
    CommProtocol ProtocolType { get; }

    /// <summary>
    /// 获取通讯配置
    /// </summary>
    ICommunicationConfig Configuration { get; }

    /// <summary>
    /// 异步建立与设备的连接
    /// </summary>
    /// <param name="cts">取消令牌</param>
    /// <returns>表示一个异步操作任务</returns>
    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="ConnectionException"></exception>
    Task ConnectAsync(CancellationToken cts = default);

    /// <summary>
    /// 异步测试设备连接，成功后自动关闭
    /// </summary>
    /// <param name="cts">取消令牌</param>
    /// <returns>一个异步操作任务，成功返回 true，否则返回 false</returns>
    /// <exception cref="OperationCanceledException"></exception>
    Task<bool> ProbeConnectionAsync(CancellationToken cts = default);

    /// <summary>
    /// 检查设备连接状态
    /// </summary>
    /// <remarks>部分协议中的实现只能作为快速判断</remarks>
    /// <returns>连接活跃返回true，否则返回false</returns>
    bool CheckConnection();

    /// <summary>
    /// 异步尝试重新连接到设备
    /// </summary>
    /// <remarks>
    /// 在中途断线时调用此方法将尝试重新连接
    /// </remarks>
    /// <param name="maxAttempts">最多尝试次数</param>
    /// <param name="retryDelay">重试等待时间（毫秒）</param>
    /// <param name="cts">取消令牌</param>
    /// <returns>一个异步操作任务，成功连接则返回 true，否则返回 false</returns>
    /// <exception cref="OperationCanceledException"></exception>
    Task<bool> TryReconnectAsync(int maxAttempts, int retryDelay = 1000, CancellationToken cts = default);

    /// <summary>
    /// 关闭设备连接
    /// </summary>
    void Disconnect();
}

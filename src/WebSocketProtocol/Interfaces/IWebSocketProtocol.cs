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

namespace MAS.Communication.WebSocketProtocol;

/// <summary>
/// 与 WebSocket 服务端进行通信的客户端接口
/// </summary>
public interface IWebSocketProtocol : IProtocol {
    /// <summary>
    /// 当收到完整的文本或二进制消息时触发
    /// </summary>
    /// <remarks>分片消息会在内部合并完成后才触发一次事件</remarks>
    event EventHandler<WebSocketMessageReceivedEventArgs>? MessageReceived;

    /// <summary>
    /// 当连接关闭（远端关闭、主动断开或异常断线）时触发
    /// </summary>
    event EventHandler<WebSocketClosedEventArgs>? Closed;

    /// <summary>
    /// 当异常断线后自动重连成功时触发
    /// </summary>
    event EventHandler? Reconnected;

    /// <summary>
    /// 获取当前连接状态
    /// </summary>
    /// <remarks>尚未建立过连接时为 <see cref="WebSocketState.None"/></remarks>
    WebSocketState State { get; }

    /// <summary>
    /// 异步发送文本消息（UTF-8 编码）
    /// </summary>
    /// <param name="message">要发送的文本内容</param>
    /// <param name="token">取消令牌</param>
    /// <returns>表示一个异步操作任务</returns>
    /// <exception cref="OperationCanceledException">取消此操作时抛出此异常</exception>
    /// <exception cref="ConnectionException">连接未建立或已断开时抛出此异常</exception>
    /// <exception cref="WriteErrorException">发送失败、超时或消息超过大小上限时抛出此异常</exception>
    Task SendTextAsync(string message, CancellationToken token = default);

    /// <summary>
    /// 异步发送二进制消息
    /// </summary>
    /// <param name="message">要发送的二进制内容</param>
    /// <param name="token">取消令牌</param>
    /// <returns>表示一个异步操作任务</returns>
    /// <exception cref="OperationCanceledException">取消此操作时抛出此异常</exception>
    /// <exception cref="ConnectionException">连接未建立或已断开时抛出此异常</exception>
    /// <exception cref="WriteErrorException">发送失败、超时或消息超过大小上限时抛出此异常</exception>
    Task SendBinaryAsync(byte[] message, CancellationToken token = default);

    /// <summary>
    /// 异步执行正常关闭握手并断开连接
    /// </summary>
    /// <remarks>主动断开不会触发自动重连；关闭握手超时后将直接中止连接</remarks>
    /// <param name="token">取消令牌</param>
    /// <returns>表示一个异步操作任务</returns>
    /// <exception cref="OperationCanceledException">取消此操作时抛出此异常</exception>
    Task DisconnectAsync(CancellationToken token = default);
}

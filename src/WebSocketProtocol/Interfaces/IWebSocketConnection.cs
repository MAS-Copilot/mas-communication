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
/// 单个 WebSocket 连接的内部抽象，屏蔽 <see cref="ClientWebSocket"/> 在不同目标框架间的 API 差异，
/// 并允许单元测试注入可控实现进行确定性验证（例如可控阻塞的发送超时）
/// </summary>
internal interface IWebSocketConnection : IDisposable {
    /// <summary>
    /// 获取连接状态
    /// </summary>
    WebSocketState State { get; }

    /// <summary>
    /// 获取对端的关闭状态码；尚未关闭时为 <see langword="null"/>
    /// </summary>
    WebSocketCloseStatus? CloseStatus { get; }

    /// <summary>
    /// 获取对端的关闭描述
    /// </summary>
    string? CloseStatusDescription { get; }

    /// <summary>
    /// 异步建立连接
    /// </summary>
    Task ConnectAsync(Uri endpoint, CancellationToken token);

    /// <summary>
    /// 异步发送一条完整消息
    /// </summary>
    Task SendAsync(byte[] payload, WebSocketMessageType messageType, CancellationToken token);

    /// <summary>
    /// 异步接收一帧数据到缓冲区
    /// </summary>
    Task<WebSocketFrame> ReceiveAsync(byte[] buffer, CancellationToken token);

    /// <summary>
    /// 异步发送关闭帧（不等待对端确认）
    /// </summary>
    Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? description, CancellationToken token);

    /// <summary>
    /// 立即中止连接
    /// </summary>
    void Abort();
}

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
/// 一次接收操作的结果帧，屏蔽不同目标框架的接收 API 差异
/// </summary>
/// <param name="count">本次写入缓冲区的字节数</param>
/// <param name="messageType">帧所属的消息类型</param>
/// <param name="isEndOfMessage">是否为消息的最后一帧</param>
internal readonly struct WebSocketFrame(int count, WebSocketMessageType messageType, bool isEndOfMessage) {
    /// <summary>
    /// 获取本次写入缓冲区的字节数
    /// </summary>
    public int Count { get; } = count;

    /// <summary>
    /// 获取帧所属的消息类型
    /// </summary>
    public WebSocketMessageType MessageType { get; } = messageType;

    /// <summary>
    /// 获取一个值，指示是否为消息的最后一帧
    /// </summary>
    public bool EndOfMessage { get; } = isEndOfMessage;
}

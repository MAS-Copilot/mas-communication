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

/// <summary>
/// WebSocket 消息接收事件参数
/// </summary>
/// <remarks>分片消息会在内部合并完成后才触发一次事件</remarks>
/// <remarks>
/// 初始化 <see cref="WebSocketMessageReceivedEventArgs"/> 新实例
/// </remarks>
/// <param name="messageType">消息类型（文本或二进制）</param>
/// <param name="data">合并后的完整消息内容</param>
public sealed class WebSocketMessageReceivedEventArgs(WebSocketMessageType messageType, byte[] data) : EventArgs {

    /// <summary>
    /// 获取消息类型（<see cref="WebSocketMessageType.Text"/> 或 <see cref="WebSocketMessageType.Binary"/>）
    /// </summary>
    public WebSocketMessageType MessageType { get; } = messageType;

    /// <summary>
    /// 获取合并后的完整消息内容
    /// </summary>
    public byte[] Data { get; } = data;

    /// <summary>
    /// 获取文本消息内容（UTF-8 解码）；二进制消息时为 <see langword="null"/>
    /// </summary>
    public string? Text { get; } = messageType == WebSocketMessageType.Text ? Encoding.UTF8.GetString(data) : null;
}

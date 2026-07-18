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
/// WebSocket 连接关闭事件参数
/// </summary>
/// <param name="closeStatus">关闭状态码；异常断线时可能为 <see langword="null"/></param>
/// <param name="closeStatusDescription">关闭描述</param>
/// <param name="exception">导致断线的异常；正常关闭时为 <see langword="null"/></param>
public sealed class WebSocketClosedEventArgs(
    WebSocketCloseStatus? closeStatus,
    string? closeStatusDescription,
    Exception? exception) : EventArgs {
    /// <summary>
    /// 获取关闭状态码；异常断线时可能为 <see langword="null"/>
    /// </summary>
    public WebSocketCloseStatus? CloseStatus { get; } = closeStatus;

    /// <summary>
    /// 获取关闭描述
    /// </summary>
    public string? CloseStatusDescription { get; } = closeStatusDescription;

    /// <summary>
    /// 获取导致断线的异常；正常关闭时为 <see langword="null"/>
    /// </summary>
    public Exception? Exception { get; } = exception;

    /// <summary>
    /// 获取一个值，指示是否为异常断线（而非正常关闭握手）
    /// </summary>
    public bool IsAbnormal => Exception is not null;
}

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
/// OPC UA 服务调用异常
/// </summary>
/// <remarks>
/// 用于承载整体性服务错误（例如会话中断、请求无法发送）时的 OPC UA 诊断信息；
/// 批量读写中的单节点错误应通过逐项状态码返回，而非抛出此异常
/// </remarks>
public class OpcUaServiceException : Exception {
    /// <summary>
    /// 获取 OPC UA 状态码（原始 32 位无符号整数）
    /// </summary>
    public uint StatusCode { get; }

    /// <summary>
    /// 获取诊断信息
    /// </summary>
    public string? DiagnosticInfo { get; }

    /// <summary>
    /// 初始化 <see cref="OpcUaServiceException"/> 新实例
    /// </summary>
    /// <param name="message">异常消息</param>
    public OpcUaServiceException(string message) : base(message) { }

    /// <summary>
    /// 初始化 <see cref="OpcUaServiceException"/> 新实例
    /// </summary>
    /// <param name="message">异常消息</param>
    /// <param name="innerException">内部异常</param>
    public OpcUaServiceException(string message, Exception innerException) : base(message, innerException) { }

    /// <summary>
    /// 初始化 <see cref="OpcUaServiceException"/> 新实例
    /// </summary>
    /// <param name="message">异常消息</param>
    /// <param name="statusCode">OPC UA 状态码</param>
    /// <param name="diagnosticInfo">诊断信息</param>
    /// <param name="innerException">内部异常</param>
    public OpcUaServiceException(
        string message,
        uint statusCode,
        string? diagnosticInfo = null,
        Exception? innerException = null) : base(message, innerException) {
        StatusCode = statusCode;
        DiagnosticInfo = diagnosticInfo;
    }
}

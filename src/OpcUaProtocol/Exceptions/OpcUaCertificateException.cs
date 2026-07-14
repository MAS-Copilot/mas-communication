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
/// OPC UA 证书相关异常
/// </summary>
/// <remarks>用于应用证书初始化失败、服务端证书校验失败或指定客户端证书不可用等场景</remarks>
public class OpcUaCertificateException : Exception {
    /// <summary>
    /// 初始化 <see cref="OpcUaCertificateException"/> 新实例
    /// </summary>
    /// <param name="message">异常消息</param>
    public OpcUaCertificateException(string message) : base(message) { }

    /// <summary>
    /// 初始化 <see cref="OpcUaCertificateException"/> 新实例
    /// </summary>
    /// <param name="message">异常消息</param>
    /// <param name="innerException">内部异常</param>
    public OpcUaCertificateException(string message, Exception innerException) : base(message, innerException) { }
}

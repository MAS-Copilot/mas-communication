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
/// OPC UA 消息安全模式
/// </summary>
/// <remarks>与端点选择配合使用，决定客户端与服务端之间的消息保护级别</remarks>
public enum OpcUaSecurityMode {
    /// <summary>
    /// 不进行任何签名或加密（不推荐用于生产环境）
    /// </summary>
    None,

    /// <summary>
    /// 仅对消息进行签名，保证完整性，但不加密
    /// </summary>
    Sign,

    /// <summary>
    /// 对消息进行签名并加密，同时保证完整性与机密性
    /// </summary>
    SignAndEncrypt
}

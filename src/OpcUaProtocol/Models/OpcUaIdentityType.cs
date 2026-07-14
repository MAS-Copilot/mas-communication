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
/// OPC UA 客户端身份类型
/// </summary>
public enum OpcUaIdentityType {
    /// <summary>
    /// 匿名身份
    /// </summary>
    Anonymous,

    /// <summary>
    /// 用户名/密码身份，密码通过 <see cref="IOpcUaCredentialProvider"/> 解析
    /// </summary>
    UserName,

    /// <summary>
    /// 客户端证书身份，使用 <see cref="IOpcUaCommunicationConfig.ClientCertificateThumbprint"/> 指定证书
    /// </summary>
    Certificate
}

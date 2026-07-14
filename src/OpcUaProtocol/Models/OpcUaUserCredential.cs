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
/// OPC UA 用户名/密码凭据
/// </summary>
/// <remarks>
/// 由 <see cref="IOpcUaCredentialProvider"/> 在建立会话时按需解析，凭据本身不会进入配置对象、
/// 实例键、日志或异常信息
/// </remarks>
/// <param name="UserName">用户名</param>
/// <param name="Password">密码；可为 <see langword="null"/> 表示空密码</param>
public sealed record OpcUaUserCredential(string UserName, string? Password);

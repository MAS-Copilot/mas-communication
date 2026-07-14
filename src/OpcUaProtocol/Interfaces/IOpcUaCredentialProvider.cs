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
/// OPC UA 凭据提供者
/// </summary>
/// <remarks>
/// 用于在建立会话时按 <see cref="IOpcUaCommunicationConfig.CredentialKey"/> 解析用户名/密码，
/// 从而避免将密码保存进配置对象、实例键、日志或异常信息
/// </remarks>
public interface IOpcUaCredentialProvider {
    /// <summary>
    /// 根据凭据引用键异步解析用户名/密码凭据
    /// </summary>
    /// <param name="credentialKey">凭据引用键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>解析出的凭据；无法解析时返回 <see langword="null"/></returns>
    /// <exception cref="OperationCanceledException">取消此操作时抛出此异常</exception>
    ValueTask<OpcUaUserCredential?> GetCredentialAsync(
        string credentialKey,
        CancellationToken cancellationToken = default);
}

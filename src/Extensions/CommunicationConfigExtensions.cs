// =============================================================================
// MAS.Communication
// https://www.mas-automation.com/
//
// Copyright 2026 MAS (厦门威光) Corporation
//
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.
// =============================================================================

using MAS.Communication.McProtocol;
using MAS.Communication.ModbusProtocol;
using MAS.Communication.OpcUaProtocol;
using MAS.Communication.S7Protocol;

namespace MAS.Communication;

/// <summary>
/// 通讯配置扩展方法
/// </summary>
public static class CommunicationConfigExtensions {
    /// <summary>
    /// 获取通讯实例键
    /// </summary>
    /// <param name="config">通讯配置</param>
    /// <returns>唯一实例键</returns>
    /// <exception cref="NotSupportedException"></exception>
    public static string GetInstanceKey(this ICommunicationConfig config) {
        return config switch {
            IS7CommunicationConfig s7 => $"{s7.Type}-{s7.Ip}-{s7.Rack}-{s7.Slot}",
            IMcCommunicationConfig mc => $"{mc.ProtocolName}-{mc.ProtocolFrame}-{mc.Ip}-{mc.Port}",
            IModbusCommunicationConfig mb => $"{mb.ProtocolName}-{mb.Ip}-{mb.Port}-{mb.UnitId}",
            IOpcUaCommunicationConfig opcUa => BuildOpcUaInstanceKey(opcUa),
            _ => throw new NotSupportedException($"Unsupported communication config type: {config.GetType().Name}")
        };
    }

    private static string BuildOpcUaInstanceKey(IOpcUaCommunicationConfig config) {
        // 身份标识只使用非敏感字段；密码不参与实例键
        string identityId = config.IdentityType switch {
            OpcUaIdentityType.UserName => config.UserName ?? string.Empty,
            OpcUaIdentityType.Certificate => config.ClientCertificateThumbprint ?? string.Empty,
            _ => string.Empty
        };

        return $"OpcUa|{NormalizeEndpointUrl(config.EndpointUrl)}|{config.SecurityMode}|{config.SecurityPolicyUri}|{config.IdentityType}|{identityId}";
    }

    private static string NormalizeEndpointUrl(string endpointUrl) {
        if (string.IsNullOrEmpty(endpointUrl)) {
            return string.Empty;
        }

        return endpointUrl.Trim().TrimEnd('/').ToLowerInvariant();
    }
}

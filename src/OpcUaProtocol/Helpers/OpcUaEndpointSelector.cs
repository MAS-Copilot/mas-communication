// =============================================================================
// MAS.Communication
// https://www.mas-automation.com/
//
// Copyright 2026 MAS (厦门威光) Corporation
//
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.
// =============================================================================

using Opc.Ua;
using Opc.Ua.Client;

// 使用官方客户端的稳定同步友好重载；对应的遥测（ITelemetryContext）新 API 暂不在首版范围内
#pragma warning disable CS0618

namespace MAS.Communication.OpcUaProtocol;

/// <summary>
/// 根据配置的安全模式与安全策略选择 OPC UA 端点的内部帮助类
/// </summary>
internal static class OpcUaEndpointSelector {
    /// <summary>
    /// 发现并选择满足安全模式与安全策略要求的端点
    /// </summary>
    /// <param name="appConfig">应用配置</param>
    /// <param name="config">OPC UA 通信配置</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>已配置的端点</returns>
    /// <exception cref="OpcUaServiceException">找不到满足要求的端点时抛出</exception>
    public static async Task<ConfiguredEndpoint> SelectAsync(
        ApplicationConfiguration appConfig,
        IOpcUaCommunicationConfig config,
        CancellationToken cancellationToken = default) {
        MessageSecurityMode desiredMode = MapSecurityMode(config.SecurityMode);
        EndpointConfiguration endpointConfiguration = EndpointConfiguration.Create(appConfig);

        EndpointDescription? selected = await DiscoverMatchingEndpointAsync(
            appConfig, config, desiredMode, endpointConfiguration, cancellationToken).ConfigureAwait(false);

        if (selected is null && config.UseEndpointDiscovery) {
            bool useSecurity = config.SecurityMode != OpcUaSecurityMode.None;
            selected = CoreClientUtils.SelectEndpoint(appConfig, config.EndpointUrl, useSecurity);
        }

        if (selected is null) {
            throw new OpcUaServiceException(
                $"未找到匹配的端点。EndpointUrl={config.EndpointUrl}, SecurityMode={config.SecurityMode}, SecurityPolicyUri={config.SecurityPolicyUri}。");
        }

        return new ConfiguredEndpoint(null, selected, endpointConfiguration);
    }

    /// <summary>
    /// 将库自有安全模式映射为 SDK 消息安全模式
    /// </summary>
    public static MessageSecurityMode MapSecurityMode(OpcUaSecurityMode mode) {
        return mode switch {
            OpcUaSecurityMode.None => MessageSecurityMode.None,
            OpcUaSecurityMode.Sign => MessageSecurityMode.Sign,
            OpcUaSecurityMode.SignAndEncrypt => MessageSecurityMode.SignAndEncrypt,
            _ => MessageSecurityMode.None
        };
    }

    private static async Task<EndpointDescription?> DiscoverMatchingEndpointAsync(
        ApplicationConfiguration appConfig,
        IOpcUaCommunicationConfig config,
        MessageSecurityMode desiredMode,
        EndpointConfiguration endpointConfiguration,
        CancellationToken cancellationToken) {
        DiscoveryClient client = await DiscoveryClient.CreateAsync(appConfig, new Uri(config.EndpointUrl), endpointConfiguration, ct: cancellationToken);
        try {
            EndpointDescriptionCollection endpoints =
                await client.GetEndpointsAsync(null, cancellationToken).ConfigureAwait(false);

            IEnumerable<EndpointDescription> candidates = endpoints
                .Where(e => e.SecurityMode == desiredMode);

            if (!string.IsNullOrEmpty(config.SecurityPolicyUri)) {
                candidates = candidates.Where(e =>
                    string.Equals(e.SecurityPolicyUri, config.SecurityPolicyUri, StringComparison.Ordinal));
            }

            return candidates
                .OrderByDescending(e => e.SecurityLevel)
                .FirstOrDefault();
        } finally {
            _ = await client.CloseAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}

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
/// OPC UA 通信配置接口
/// </summary>
/// <remarks>
/// <para>
/// <see cref="EndpointUrl"/> 是连接主键（例如 <c>opc.tcp://192.168.1.10:4840/Server</c>）。
/// 基类继承的 <see cref="ICommunicationConfig.Ip"/> 仅为兼容现有框架，OPC UA 地址包含协议、
/// 端口与路径，不能仅依赖 <see cref="ICommunicationConfig.Ip"/>。
/// </para>
/// <para>
/// 出于安全考虑，密码不应进入配置对象、实例键、日志或异常信息，推荐通过
/// <see cref="CredentialKey"/> 配合 <see cref="CredentialProvider"/> 在建立会话时按需解析。
/// </para>
/// <para>
/// 配置会在客户端创建时克隆为快照。请确保 <c>Clone</c> 实现完整复制以下所有字段
/// （包括 <see cref="CredentialProvider"/> 引用），否则可能导致实例无法从管理器缓存中正确移除。
/// </para>
/// </remarks>
public interface IOpcUaCommunicationConfig : ICommunicationConfig {
    /// <summary>
    /// 获取或设置端点地址，是 OPC UA 的连接主键
    /// </summary>
    /// <remarks>例如 <c>opc.tcp://192.168.1.10:4840/Server</c></remarks>
    string EndpointUrl { get; set; }

    /// <summary>
    /// 获取或设置客户端应用名称
    /// </summary>
    string ApplicationName { get; set; }

    /// <summary>
    /// 获取或设置客户端应用 URI
    /// </summary>
    /// <remarks>应保证在证书中一致，通常形如 <c>urn:HOST:MAS:Communication:Client</c></remarks>
    string ApplicationUri { get; set; }

    /// <summary>
    /// 获取或设置消息安全模式，参与端点选择
    /// </summary>
    OpcUaSecurityMode SecurityMode { get; set; }

    /// <summary>
    /// 获取或设置安全策略 URI，参与端点选择
    /// </summary>
    /// <remarks>例如 <c>http://opcfoundation.org/UA/SecurityPolicy#Basic256Sha256</c></remarks>
    string SecurityPolicyUri { get; set; }

    /// <summary>
    /// 获取或设置客户端身份类型
    /// </summary>
    OpcUaIdentityType IdentityType { get; set; }

    /// <summary>
    /// 获取或设置用户名（仅当 <see cref="IdentityType"/> 为 <see cref="OpcUaIdentityType.UserName"/> 时使用）
    /// </summary>
    string? UserName { get; set; }

    /// <summary>
    /// 获取或设置凭据引用键
    /// </summary>
    /// <remarks>保存凭据引用而非密码本身，配合 <see cref="CredentialProvider"/> 解析实际密码</remarks>
    string? CredentialKey { get; set; }

    /// <summary>
    /// 获取凭据提供者
    /// </summary>
    /// <remarks>
    /// 该属性承载的是服务引用而非配置数据，不参与实例键计算；
    /// 当 <see cref="IdentityType"/> 为 <see cref="OpcUaIdentityType.UserName"/> 时用于解析密码
    /// </remarks>
    IOpcUaCredentialProvider? CredentialProvider { get; }

    /// <summary>
    /// 获取或设置客户端证书指纹（仅当 <see cref="IdentityType"/> 为 <see cref="OpcUaIdentityType.Certificate"/> 时使用）
    /// </summary>
    string? ClientCertificateThumbprint { get; set; }

    /// <summary>
    /// 获取或设置证书存储根路径（PKI 目录）
    /// </summary>
    string CertificateStorePath { get; set; }

    /// <summary>
    /// 获取或设置一个值，指示是否自动接受不受信任的服务端证书
    /// </summary>
    /// <remarks>默认必须为 <see langword="false"/>，仅建议在受控的开发环境中临时开启</remarks>
    bool AutoAcceptUntrustedCertificates { get; set; }

    /// <summary>
    /// 获取或设置会话超时时间（毫秒）
    /// </summary>
    int SessionTimeout { get; set; }

    /// <summary>
    /// 获取或设置保活间隔（毫秒）
    /// </summary>
    int KeepAliveInterval { get; set; }

    /// <summary>
    /// 获取或设置重连等待时间（毫秒）
    /// </summary>
    int ReconnectDelay { get; set; }

    /// <summary>
    /// 获取或设置一个值，指示是否使用端点发现服务
    /// </summary>
    bool UseEndpointDiscovery { get; set; }
}

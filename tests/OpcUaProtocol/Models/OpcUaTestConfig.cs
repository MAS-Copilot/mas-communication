// =============================================================================
// MAS.Communication
// https://www.mas-automation.com/
//
// Copyright 2026 MAS (厦门威光) Corporation
//
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.
// =============================================================================

using MAS.Communication;
using MAS.Communication.OpcUaProtocol;

namespace MAS.CommunicationUnitTest.OpcUaProtocol.Models;

/// <summary>
/// 用于测试的 OPC UA 配置实现，同时演示调用方应如何实现 <see cref="IOpcUaCommunicationConfig"/> 与 <c>Clone</c>
/// </summary>
public sealed class OpcUaTestConfig : IOpcUaCommunicationConfig {
    public string ProtocolName => "OpcUa";

    public string Ip { get; set; } = "127.0.0.1";
    public int MaxRetries { get; set; } = 3;
    public int ReadTimeout { get; set; } = 5000;
    public int WriteTimeout { get; set; } = 5000;

    public string EndpointUrl { get; set; } = "opc.tcp://127.0.0.1:4840";
    public string ApplicationName { get; set; } = "MAS.Communication.Tests";
    public string ApplicationUri { get; set; } = "urn:localhost:MAS:Communication:Tests";

    public OpcUaSecurityMode SecurityMode { get; set; } = OpcUaSecurityMode.None;
    public string SecurityPolicyUri { get; set; } = string.Empty;

    public OpcUaIdentityType IdentityType { get; set; } = OpcUaIdentityType.Anonymous;
    public string? UserName { get; set; }
    public string? CredentialKey { get; set; }
    public IOpcUaCredentialProvider? CredentialProvider { get; set; }

    public string? ClientCertificateThumbprint { get; set; }
    public string CertificateStorePath { get; set; } = string.Empty;

    public bool AutoAcceptUntrustedCertificates { get; set; }

    public int SessionTimeout { get; set; } = 60000;
    public int KeepAliveInterval { get; set; } = 5000;
    public int ReconnectDelay { get; set; } = 1000;

    public bool UseEndpointDiscovery { get; set; } = true;

    public T Clone<T>() where T : ICommunicationConfig {
        return (T)Clone();
    }

    public object Clone() {
        return new OpcUaTestConfig {
            Ip = Ip,
            MaxRetries = MaxRetries,
            ReadTimeout = ReadTimeout,
            WriteTimeout = WriteTimeout,
            EndpointUrl = EndpointUrl,
            ApplicationName = ApplicationName,
            ApplicationUri = ApplicationUri,
            SecurityMode = SecurityMode,
            SecurityPolicyUri = SecurityPolicyUri,
            IdentityType = IdentityType,
            UserName = UserName,
            CredentialKey = CredentialKey,
            CredentialProvider = CredentialProvider,
            ClientCertificateThumbprint = ClientCertificateThumbprint,
            CertificateStorePath = CertificateStorePath,
            AutoAcceptUntrustedCertificates = AutoAcceptUntrustedCertificates,
            SessionTimeout = SessionTimeout,
            KeepAliveInterval = KeepAliveInterval,
            ReconnectDelay = ReconnectDelay,
            UseEndpointDiscovery = UseEndpointDiscovery
        };
    }
}

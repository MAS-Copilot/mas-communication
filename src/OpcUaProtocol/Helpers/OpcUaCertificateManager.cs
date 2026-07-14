// =============================================================================
// MAS.Communication
// https://www.mas-automation.com/
//
// Copyright 2026 MAS (厦门威光) Corporation
//
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.
// =============================================================================

using System.Security.Cryptography.X509Certificates;
using Opc.Ua;
using Opc.Ua.Configuration;

// 使用官方客户端的稳定同步友好重载；对应的遥测（ITelemetryContext）新 API 暂不在首版范围内
#pragma warning disable CS0618

namespace MAS.Communication.OpcUaProtocol;

/// <summary>
/// 负责 OPC UA 应用证书初始化与客户端证书加载的内部帮助类
/// </summary>
internal static class OpcUaCertificateManager {
    private const string PRODUCT_URI = "urn:MAS:Communication:OpcUaClient";
    private const string DEFAULT_CERTIFICATE_STORE_PATH = "%LocalApplicationData%/MAS.Communication/pki";

    /// <summary>
    /// 创建 OPC UA 应用实例与应用配置，并确保应用证书就绪
    /// </summary>
    /// <param name="config">OPC UA 通信配置</param>
    /// <returns>应用实例与对应的应用配置</returns>
    /// <exception cref="OpcUaCertificateException">应用证书初始化失败时抛出</exception>
    public static async Task<(ApplicationInstance Application, ApplicationConfiguration Configuration)> CreateApplicationAsync(
        IOpcUaCommunicationConfig config) {
        string applicationUri = string.IsNullOrEmpty(config.ApplicationUri)
            ? $"urn:{Utils.GetHostName()}:MAS:Communication:OpcUaClient"
            : config.ApplicationUri;

        string pkiRoot = string.IsNullOrEmpty(config.CertificateStorePath)
            ? DEFAULT_CERTIFICATE_STORE_PATH
            : config.CertificateStorePath;

        ApplicationInstance application = new() {
            ApplicationName = config.ApplicationName,
            ApplicationType = ApplicationType.Client
        };

        ApplicationConfiguration appConfig;
        try {
            appConfig = await application
                .Build(applicationUri, PRODUCT_URI)
                .AsClient()
                .AddSecurityConfiguration($"CN={config.ApplicationName}", pkiRoot)
                .SetAutoAcceptUntrustedCertificates(config.AutoAcceptUntrustedCertificates)
                .CreateAsync()
                .ConfigureAwait(false);
        } catch (Exception ex) {
            throw new OpcUaCertificateException($"OPC UA 应用配置构建失败：{ex.Message}", ex);
        }

        bool canAutoAccept = config.AutoAcceptUntrustedCertificates;
        appConfig.CertificateValidator.CertificateValidation += (_, e) => {
            if (canAutoAccept &&
                e.Error is not null &&
                e.Error.StatusCode == StatusCodes.BadCertificateUntrusted) {
                e.Accept = true;
            }
        };

        try {
            bool hasValidCertificate = await application
                .CheckApplicationInstanceCertificatesAsync(true)
                .ConfigureAwait(false);

            if (!hasValidCertificate) {
                throw new OpcUaCertificateException("OPC UA 应用实例证书无效或缺失，且无法自动创建。");
            }
        } catch (OpcUaCertificateException) {
            throw;
        } catch (Exception ex) {
            throw new OpcUaCertificateException($"OPC UA 应用实例证书初始化失败：{ex.Message}", ex);
        }

        return (application, appConfig);
    }

    /// <summary>
    /// 根据指纹加载用于身份认证的客户端证书
    /// </summary>
    /// <param name="appConfig">应用配置</param>
    /// <param name="config">OPC UA 通信配置</param>
    /// <returns>客户端证书</returns>
    /// <exception cref="OpcUaCertificateException">找不到指定证书或证书不可用时抛出</exception>
    public static async Task<X509Certificate2> LoadClientCertificateAsync(
        ApplicationConfiguration appConfig,
        IOpcUaCommunicationConfig config) {
        if (string.IsNullOrEmpty(config.ClientCertificateThumbprint)) {
            throw new OpcUaCertificateException("身份类型为证书时必须提供 ClientCertificateThumbprint。");
        }

        CertificateIdentifier appCertificate = appConfig.SecurityConfiguration.ApplicationCertificate;
        CertificateIdentifier identifier = new() {
            StoreType = appCertificate.StoreType,
            StorePath = appCertificate.StorePath,
            Thumbprint = config.ClientCertificateThumbprint
        };

        X509Certificate2? certificate;
        try {
            certificate = await identifier.Find(true, appConfig.ApplicationUri).ConfigureAwait(false);
        } catch (Exception ex) {
            throw new OpcUaCertificateException(
                $"加载客户端证书失败。Thumbprint={config.ClientCertificateThumbprint}：{ex.Message}", ex);
        }

        if (certificate is null) {
            throw new OpcUaCertificateException(
                $"未找到指定的客户端证书。Thumbprint={config.ClientCertificateThumbprint}，StorePath={identifier.StorePath}。");
        }

        return certificate;
    }
}

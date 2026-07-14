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
using MAS.CommunicationUnitTest.OpcUaProtocol.Models;

namespace MAS.CommunicationUnitTest.OpcUaProtocol;

[TestClass]
public sealed class OpcUaInstanceKeyTest {
    [TestMethod]
    public void 匿名身份实例键格式() {
        OpcUaTestConfig config = new() {
            EndpointUrl = "opc.tcp://192.168.1.10:4840/Server",
            SecurityMode = OpcUaSecurityMode.None,
            SecurityPolicyUri = string.Empty,
            IdentityType = OpcUaIdentityType.Anonymous
        };

        string key = config.GetInstanceKey();

        Assert.AreEqual("OpcUa|opc.tcp://192.168.1.10:4840/server|None||Anonymous|", key);
    }

    [TestMethod]
    public void 用户名身份使用用户名作为身份标识() {
        OpcUaTestConfig config = new() {
            EndpointUrl = "opc.tcp://192.168.1.10:4840/server",
            SecurityMode = OpcUaSecurityMode.SignAndEncrypt,
            SecurityPolicyUri = "Basic256Sha256",
            IdentityType = OpcUaIdentityType.UserName,
            UserName = "operator",
            CredentialKey = "vault://opcua/secret",
            ReadTimeout = 3000
        };

        string key = config.GetInstanceKey();

        Assert.AreEqual("OpcUa|opc.tcp://192.168.1.10:4840/server|SignAndEncrypt|Basic256Sha256|UserName|operator", key);
    }

    [TestMethod]
    public void 证书身份使用指纹作为身份标识() {
        OpcUaTestConfig config = new() {
            EndpointUrl = "opc.tcp://plc:4840",
            SecurityMode = OpcUaSecurityMode.Sign,
            SecurityPolicyUri = "Basic256Sha256",
            IdentityType = OpcUaIdentityType.Certificate,
            ClientCertificateThumbprint = "ABCDEF0123456789"
        };

        string key = config.GetInstanceKey();

        Assert.AreEqual("OpcUa|opc.tcp://plc:4840|Sign|Basic256Sha256|Certificate|ABCDEF0123456789", key);
    }

    [TestMethod]
    public void 实例键不包含凭据引用键() {
        OpcUaTestConfig config = new() {
            IdentityType = OpcUaIdentityType.UserName,
            UserName = "operator",
            CredentialKey = "super-secret-credential-key"
        };

        string key = config.GetInstanceKey();

        Assert.IsFalse(key.Contains("super-secret-credential-key", StringComparison.Ordinal));
    }

    [TestMethod]
    public void 端点地址规范化_去除尾部斜杠并转小写() {
        OpcUaTestConfig withSlash = new() { EndpointUrl = "OPC.TCP://Server:4840/Path/" };
        OpcUaTestConfig withoutSlash = new() { EndpointUrl = "opc.tcp://server:4840/path" };

        Assert.AreEqual(withoutSlash.GetInstanceKey(), withSlash.GetInstanceKey());
    }
}

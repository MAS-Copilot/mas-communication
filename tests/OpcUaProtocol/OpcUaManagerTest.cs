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
using Microsoft.Extensions.DependencyInjection;

namespace MAS.CommunicationUnitTest.OpcUaProtocol;

[TestClass]
public sealed class OpcUaManagerTest {
    private static IProtocolManager CreateManager() {
        ServiceCollection services = new();
        _ = services.AddCommunication();
        return services.BuildServiceProvider().GetRequiredService<IProtocolManager>();
    }

    [TestMethod]
    public void 管理器为OPC_UA配置创建强类型实例() {
        using IProtocolManager manager = CreateManager();
        OpcUaTestConfig config = new();

        IOpcUaProtocol protocol = manager.GetOrCreate<IOpcUaProtocol>(config);

        Assert.IsNotNull(protocol);
        Assert.AreEqual(CommProtocol.OpcUa, protocol.ProtocolType);
    }

    [TestMethod]
    public void 相同配置复用同一实例() {
        using IProtocolManager manager = CreateManager();
        OpcUaTestConfig config = new();

        IProtocol first = manager.GetOrCreate(config);
        IProtocol second = manager.GetOrCreate(config);

        Assert.AreSame(first, second);
        Assert.AreEqual(1, manager.Count);
    }

    [TestMethod]
    public void 不同端点创建不同实例() {
        using IProtocolManager manager = CreateManager();
        OpcUaTestConfig first = new() { EndpointUrl = "opc.tcp://server-a:4840" };
        OpcUaTestConfig second = new() { EndpointUrl = "opc.tcp://server-b:4840" };

        _ = manager.GetOrCreate(first);
        _ = manager.GetOrCreate(second);

        Assert.AreEqual(2, manager.Count);
    }

    [TestMethod]
    public void 配置在创建后被克隆为快照() {
        using IProtocolManager manager = CreateManager();
        OpcUaTestConfig config = new() { EndpointUrl = "opc.tcp://server:4840" };

        IProtocol protocol = manager.GetOrCreate(config);
        string keyBefore = protocol.Configuration.GetInstanceKey();

        // 调用方在创建后修改原始配置，不应影响已缓存实例的实例键
        config.EndpointUrl = "opc.tcp://another-server:9999";

        string keyAfter = protocol.Configuration.GetInstanceKey();
        Assert.AreEqual(keyBefore, keyAfter);

        // 由于实例持有快照，仍可通过原始配置正确移除
        Assert.IsTrue(manager.Remove(protocol));
        Assert.AreEqual(0, manager.Count);
    }

    [TestMethod]
    public void 释放实例后从管理器缓存移除() {
        using IProtocolManager manager = CreateManager();
        OpcUaTestConfig config = new();

        IProtocol protocol = manager.GetOrCreate(config);
        Assert.AreEqual(1, manager.Count);

        protocol.Dispose();

        Assert.AreEqual(0, manager.Count);
    }
}

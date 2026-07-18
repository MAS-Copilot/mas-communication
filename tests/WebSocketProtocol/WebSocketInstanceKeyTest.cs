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
using MAS.Communication.WebSocketProtocol;
using MAS.CommunicationUnitTest.WebSocketProtocol.Models;

namespace MAS.CommunicationUnitTest.WebSocketProtocol;

/// <summary>
/// WebSocket 实例键与 <see cref="ProtocolManager"/> 实例管理测试（无需真实服务端）
/// </summary>
[TestClass]
public sealed class WebSocketInstanceKeyTest {
    [TestMethod]
    public void 相同配置复用同一实例() {
        using ProtocolManager manager = new();
        WebSocketTestConfig first = new() { EndpointUrl = "ws://127.0.0.1:18080/data", SubProtocol = "mas.v1" };
        WebSocketTestConfig second = new() { EndpointUrl = "ws://127.0.0.1:18080/data", SubProtocol = "mas.v1" };

        IProtocol protocol1 = manager.GetOrCreate(first);
        IProtocol protocol2 = manager.GetOrCreate(second);

        Assert.AreSame(protocol1, protocol2);
        Assert.AreEqual(1, manager.Count);
    }

    [TestMethod]
    public void 不同URL创建不同实例() {
        using ProtocolManager manager = new();
        WebSocketTestConfig first = new() { EndpointUrl = "ws://127.0.0.1:18080/a" };
        WebSocketTestConfig second = new() { EndpointUrl = "ws://127.0.0.1:18080/b" };

        IProtocol protocol1 = manager.GetOrCreate(first);
        IProtocol protocol2 = manager.GetOrCreate(second);

        Assert.AreNotSame(protocol1, protocol2);
        Assert.AreEqual(2, manager.Count);
    }

    [TestMethod]
    public void 不同SubProtocol创建不同实例() {
        using ProtocolManager manager = new();
        WebSocketTestConfig first = new() { EndpointUrl = "ws://127.0.0.1:18080/data", SubProtocol = "mas.v1" };
        WebSocketTestConfig second = new() { EndpointUrl = "ws://127.0.0.1:18080/data", SubProtocol = "mas.v2" };

        IProtocol protocol1 = manager.GetOrCreate(first);
        IProtocol protocol2 = manager.GetOrCreate(second);

        Assert.AreNotSame(protocol1, protocol2);
        Assert.AreEqual(2, manager.Count);
    }

    [TestMethod]
    public void 实例键规范化Scheme和Host大小写与默认端口() {
        WebSocketTestConfig first = new() { EndpointUrl = "WS://Gateway.Example.COM/Data" };
        WebSocketTestConfig second = new() { EndpointUrl = "ws://gateway.example.com:80/Data" };

        Assert.AreEqual(second.GetInstanceKey(), first.GetInstanceKey());
    }

    [TestMethod]
    public void 实例键保留Path大小写与尾斜杠() {
        WebSocketTestConfig upperPath = new() { EndpointUrl = "ws://host:8080/Device/A" };
        WebSocketTestConfig lowerPath = new() { EndpointUrl = "ws://host:8080/device/a" };
        Assert.AreNotEqual(lowerPath.GetInstanceKey(), upperPath.GetInstanceKey(), "path 大小写可能有语义，不允许合并");

        WebSocketTestConfig trailingSlash = new() { EndpointUrl = "ws://host:8080/device/a/" };
        Assert.AreNotEqual(lowerPath.GetInstanceKey(), trailingSlash.GetInstanceKey(), "尾斜杠可能有语义，不允许合并");
    }

    [TestMethod]
    public void 不同ConnectionIdentity创建不同实例() {
        using ProtocolManager manager = new();
        WebSocketTestConfig first = new() { EndpointUrl = "ws://127.0.0.1:18080/data", ConnectionIdentity = "tenant-a" };
        WebSocketTestConfig second = new() { EndpointUrl = "ws://127.0.0.1:18080/data", ConnectionIdentity = "tenant-b" };

        IProtocol protocol1 = manager.GetOrCreate(first);
        IProtocol protocol2 = manager.GetOrCreate(second);

        Assert.AreNotSame(protocol1, protocol2, "不同认证身份不允许复用同一连接");
        Assert.AreEqual(2, manager.Count);
    }

    [TestMethod]
    public void 敏感Header不参与实例键() {
        WebSocketTestConfig withToken = new() {
            EndpointUrl = "ws://127.0.0.1:18080/data",
            Headers = new Dictionary<string, string> { ["Authorization"] = "Bearer secret-token" }
        };
        WebSocketTestConfig withoutToken = new() { EndpointUrl = "ws://127.0.0.1:18080/data" };

        string key = withToken.GetInstanceKey();

        Assert.AreEqual(withoutToken.GetInstanceKey(), key);
        Assert.DoesNotContain("secret-token", key, "Token 等敏感信息不允许进入实例键");
    }

    [TestMethod]
    public void 强类型获取WebSocket协议实例() {
        using ProtocolManager manager = new();
        WebSocketTestConfig config = new() { EndpointUrl = "ws://127.0.0.1:18080/data" };

        IWebSocketProtocol protocol = manager.GetOrCreate<IWebSocketProtocol>(config);

        Assert.AreEqual(CommProtocol.WebSocket, protocol.ProtocolType);
        Assert.AreEqual(System.Net.WebSockets.WebSocketState.None, protocol.State);
    }

    [TestMethod]
    public void 释放后从ProtocolManager移除() {
        using ProtocolManager manager = new();
        WebSocketTestConfig config = new() { EndpointUrl = "ws://127.0.0.1:18080/data" };

        IProtocol protocol = manager.GetOrCreate(config);
        Assert.IsTrue(manager.Contains(protocol));

        protocol.Dispose();

        Assert.IsFalse(manager.Contains(protocol));
        Assert.AreEqual(0, manager.Count);
    }

    [TestMethod]
    public void Dispose可重复调用() {
        WebSocketTestConfig config = new() { EndpointUrl = "ws://127.0.0.1:18080/data" };
        WebSocketProtocolClient client = new(config);

        client.Dispose();
        client.Dispose();

        Assert.AreEqual(System.Net.WebSockets.WebSocketState.None, client.State);
    }
}

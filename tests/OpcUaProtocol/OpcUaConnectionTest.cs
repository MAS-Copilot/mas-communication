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

/// <summary>
/// 需要真实 OPC UA 服务端的集成测试；未配置或无法连接时将标记为 Inconclusive。
/// 通过环境变量 <c>MAS_OPCUA_ENDPOINT</c> 指定端点地址，默认 <c>opc.tcp://localhost:4840</c>。
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class OpcUaConnectionTest : IDisposable {
    // OPC UA 标准节点：Server_ServerStatus_CurrentTime，命名空间 0，任意服务端均存在
    private const string CURRENT_TIME_NODE = "i=2258";

    private readonly OpcUaTestConfig _config;
    private readonly OpcUaProtocolClient _protocol;
    private bool _disposed;

    public TestContext TestContext { get; set; }

    public OpcUaConnectionTest() {
        string endpoint = Environment.GetEnvironmentVariable("MAS_OPCUA_ENDPOINT") ?? "opc.tcp://localhost:4840";

        _config = new OpcUaTestConfig {
            EndpointUrl = endpoint,
            SecurityMode = OpcUaSecurityMode.None,
            IdentityType = OpcUaIdentityType.Anonymous,
            AutoAcceptUntrustedCertificates = true,
            UseEndpointDiscovery = true
        };

        _protocol = new OpcUaProtocolClient(_config);
    }

    [TestInitialize]
    public async Task Initialize() {
        TestContext.WriteLine($"尝试连接 OPC UA 服务端：{_config.EndpointUrl}");

        try {
            await _protocol.ConnectAsync(TestContext.CancellationTokenSource.Token).ConfigureAwait(false);
        } catch (ConnectionException ex) {
            Assert.Inconclusive($"{ex.Message}，测试结束");
        }

        if (!_protocol.CheckConnection()) {
            Assert.Inconclusive("未能连接到 OPC UA 服务端，测试结束");
        }
    }

    [TestMethod]
    public async Task 读取标准节点当前时间() {
        OpcUaValue value = await _protocol
            .ReadAsync(new OpcUaNodeId(CURRENT_TIME_NODE), TestContext.CancellationTokenSource.Token)
            .ConfigureAwait(false);

        TestContext.WriteLine($"读取到 CurrentTime = {value.Value}，StatusCode = 0x{value.StatusCode:X8}");
        Assert.IsTrue(value.IsGood, "读取标准节点应返回良好状态码");
        Assert.IsInstanceOfType<DateTime>(value.Value);
    }

    [TestMethod]
    public async Task 泛型读取转换为DateTime() {
        DateTime time = await _protocol
            .ReadAsync<DateTime>(new OpcUaNodeId(CURRENT_TIME_NODE), TestContext.CancellationTokenSource.Token)
            .ConfigureAwait(false);

        Assert.AreNotEqual(default, time);
    }

    [TestMethod]
    public async Task 批量读取返回逐项状态() {
        IReadOnlyList<OpcUaReadItem> items = [
            new OpcUaReadItem(new OpcUaNodeId(CURRENT_TIME_NODE)),
            new OpcUaReadItem(new OpcUaNodeId("ns=0;s=不存在的节点"))
        ];

        IReadOnlyList<OpcUaValue> results = await _protocol
            .ReadAsync(items, TestContext.CancellationTokenSource.Token)
            .ConfigureAwait(false);

        Assert.HasCount(2, results);
        Assert.IsTrue(results[0].IsGood, "有效节点应返回良好状态");
        Assert.IsTrue(results[1].IsBad, "无效节点应返回错误状态，而不是让整个请求失败");
    }

    [TestMethod]
    public async Task 浏览根对象文件夹() {
        IReadOnlyList<OpcUaBrowseNode> nodes = await _protocol
            .BrowseAsync(cancellationToken: TestContext.CancellationTokenSource.Token)
            .ConfigureAwait(false);

        TestContext.WriteLine($"浏览到 {nodes.Count} 个子节点");
        Assert.IsGreaterThan(0, nodes.Count, "Objects 文件夹下应至少存在一个子节点（例如 Server）");
    }

    [TestMethod]
    public async Task 订阅数据变化() {
        using SemaphoreSlim received = new(0, 1);
        OpcUaValue? latest = null;

        IReadOnlyList<OpcUaMonitoredItem> items = [
            new OpcUaMonitoredItem(new OpcUaNodeId(CURRENT_TIME_NODE)) { SamplingInterval = 500 }
        ];

        IOpcUaSubscription subscription = await _protocol
            .SubscribeAsync(items, new OpcUaSubscriptionOptions { PublishingInterval = 500 }, TestContext.CancellationTokenSource.Token)
            .ConfigureAwait(false);

        try {
            subscription.DataChanged += (_, e) => {
                latest = e.Value;
                try {
                    _ = received.Release();
                } catch (SemaphoreFullException) {
                    // 已收到首个通知，忽略后续
                }
            };

            bool signaled = await received.WaitAsync(TimeSpan.FromSeconds(10), TestContext.CancellationTokenSource.Token).ConfigureAwait(false);

            Assert.IsTrue(signaled, "应在超时前收到至少一次数据变化通知");
            Assert.IsNotNull(latest);
            Assert.IsTrue(latest!.IsGood);
        } finally {
            await subscription.DeleteAsync(TestContext.CancellationTokenSource.Token).ConfigureAwait(false);
        }
    }

    public void Dispose() {
        if (_disposed) {
            return;
        }

        _protocol.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}

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
using MAS.CommunicationUnitTest.WebSocketProtocol.Helpers;
using MAS.CommunicationUnitTest.WebSocketProtocol.Models;
using System.Net.WebSockets;

namespace MAS.CommunicationUnitTest.WebSocketProtocol;

/// <summary>
/// WebSocket 连接建立、探测、取消与握手参数传递测试（使用本地测试服务器）
/// </summary>
[TestClass]
public sealed class WebSocketConnectionTest {
    [TestMethod]
    public async Task ConnectAsync成功后状态为Open() {
        using WebSocketTestServer server = WebSocketTestServer.Start();
        WebSocketTestConfig config = new() { EndpointUrl = server.Uri.ToString() };
        using WebSocketProtocolClient client = new(config);

        await client.ConnectAsync(TestContext.CancellationTokenSource.Token);

        Assert.AreEqual(WebSocketState.Open, client.State);
        Assert.IsTrue(client.CheckConnection());
    }

    [TestMethod]
    public async Task ProbeConnectionAsync成功后关闭且不建立主连接() {
        using WebSocketTestServer server = WebSocketTestServer.Start();
        WebSocketTestConfig config = new() { EndpointUrl = server.Uri.ToString() };
        using WebSocketProtocolClient client = new(config);

        bool isReachable = await client.ProbeConnectionAsync(TestContext.CancellationTokenSource.Token);

        Assert.IsTrue(isReachable, "本地测试服务器在线时探测应成功");
        Assert.AreEqual(WebSocketState.None, client.State, "探测不应建立主连接");
        Assert.IsFalse(client.CheckConnection());
        Assert.IsTrue(await TestWait.UntilAsync(() => server.ConnectionCount == 1));
    }

    [TestMethod]
    public async Task ProbeConnectionAsync无服务端时返回false() {
        // 先占用端口获取地址再关闭，保证端口无监听
        int port;
        using (WebSocketTestServer server = WebSocketTestServer.Start()) {
            port = server.Uri.Port;
        }

        WebSocketTestConfig config = new() { EndpointUrl = $"ws://127.0.0.1:{port}/", ConnectTimeout = 2000 };
        using WebSocketProtocolClient client = new(config);

        Assert.IsFalse(await client.ProbeConnectionAsync(TestContext.CancellationTokenSource.Token));
    }

    [TestMethod]
    public async Task 拒绝Http与Https方案() {
        WebSocketTestConfig httpConfig = new() { EndpointUrl = "http://127.0.0.1:18080/" };
        using WebSocketProtocolClient httpClient = new(httpConfig);
        _ = await Assert.ThrowsExactlyAsync<ConnectionException>(() => httpClient.ConnectAsync(TestContext.CancellationTokenSource.Token));

        WebSocketTestConfig httpsConfig = new() { EndpointUrl = "https://127.0.0.1:18080/" };
        using WebSocketProtocolClient httpsClient = new(httpsConfig);
        _ = await Assert.ThrowsExactlyAsync<ConnectionException>(() => httpsClient.ConnectAsync(TestContext.CancellationTokenSource.Token));
    }

    [TestMethod]
    public async Task 取消连接抛出OperationCanceledException() {
        using WebSocketTestServer server = WebSocketTestServer.StartSilent();
        WebSocketTestConfig config = new() { EndpointUrl = server.Uri.ToString(), ConnectTimeout = 30000 };
        using WebSocketProtocolClient client = new(config);

        using CancellationTokenSource cts = new(200);

        try {
            await client.ConnectAsync(cts.Token);
            Assert.Fail("取消令牌触发后应抛出 OperationCanceledException");
        } catch (OperationCanceledException) {
            // 预期
        }

        Assert.IsFalse(client.CheckConnection());
    }

    [TestMethod]
    public async Task 连接超时抛出ConnectionException() {
        using WebSocketTestServer server = WebSocketTestServer.StartSilent();
        WebSocketTestConfig config = new() { EndpointUrl = server.Uri.ToString(), ConnectTimeout = 300 };
        using WebSocketProtocolClient client = new(config);

        _ = await Assert.ThrowsExactlyAsync<ConnectionException>(() => client.ConnectAsync(TestContext.CancellationTokenSource.Token));
    }

    [TestMethod]
    public async Task 自定义Header与SubProtocol传递到服务端() {
        using WebSocketTestServer server = WebSocketTestServer.Start();
        WebSocketTestConfig config = new() {
            EndpointUrl = server.Uri.ToString(),
            SubProtocol = "mas.v1",
            Headers = new Dictionary<string, string> {
                ["Authorization"] = "Bearer test-token-123",
                ["X-Custom-Header"] = "mas-test"
            }
        };
        using WebSocketProtocolClient client = new(config);

        await client.ConnectAsync(TestContext.CancellationTokenSource.Token);

        Assert.IsTrue(await TestWait.UntilAsync(() => server.LastConnection is not null));
        WebSocketHandshakeInfo handshake = server.LastConnection!.Handshake;

        Assert.AreEqual("Bearer test-token-123", handshake.Headers["Authorization"]);
        Assert.AreEqual("mas-test", handshake.Headers["X-Custom-Header"]);
        Assert.IsTrue(handshake.RequestedSubProtocols.Contains("mas.v1"), "握手应携带 Sec-WebSocket-Protocol");
        Assert.AreEqual(WebSocketState.Open, client.State);
    }

    public TestContext TestContext { get; set; }
}

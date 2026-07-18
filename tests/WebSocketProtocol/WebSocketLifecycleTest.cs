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
/// WebSocket 生命周期测试：远端关闭、主动断开、异常断线自动重连（使用本地测试服务器）
/// </summary>
[TestClass]
public sealed class WebSocketLifecycleTest {
    [TestMethod]
    public async Task 远端Close触发Closed事件且不重连() {
        using WebSocketTestServer server = WebSocketTestServer.Start();
        server.Handler = async (connection, token) => {
            await connection.Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "server shutdown", token);
        };

        WebSocketTestConfig config = new() {
            EndpointUrl = server.Uri.ToString(),
            AutoReconnect = true,
            ReconnectDelay = 100,
            MaxRetries = 3
        };
        using WebSocketProtocolClient client = new(config);

        TaskCompletionSource<WebSocketClosedEventArgs> closed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        client.Closed += (_, e) => closed.TrySetResult(e);
        bool hasReconnected = false;
        client.Reconnected += (_, _) => hasReconnected = true;

        await client.ConnectAsync(TestContext.CancellationTokenSource.Token);

        WebSocketClosedEventArgs args = await closed.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationTokenSource.Token);

        Assert.AreEqual(WebSocketCloseStatus.NormalClosure, args.CloseStatus);
        Assert.IsFalse(args.IsAbnormal, "远端正常关闭不是异常断线");

        await Task.Delay(500, TestContext.CancellationTokenSource.Token);
        Assert.AreEqual(1, server.ConnectionCount, "远端正常关闭不应触发自动重连");
        Assert.IsFalse(hasReconnected);
    }

    [TestMethod]
    public async Task 主动断开不会自动重连() {
        using WebSocketTestServer server = WebSocketTestServer.Start();
        WebSocketTestConfig config = new() {
            EndpointUrl = server.Uri.ToString(),
            AutoReconnect = true,
            ReconnectDelay = 100,
            MaxRetries = 3
        };
        using WebSocketProtocolClient client = new(config);

        TaskCompletionSource<WebSocketClosedEventArgs> closed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        client.Closed += (_, e) => closed.TrySetResult(e);

        await client.ConnectAsync(TestContext.CancellationTokenSource.Token);
        await client.DisconnectAsync(TestContext.CancellationTokenSource.Token);

        WebSocketClosedEventArgs args = await closed.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationTokenSource.Token);

        Assert.IsFalse(args.IsAbnormal, "主动断开应视为正常关闭");
        Assert.IsFalse(client.CheckConnection());
        Assert.AreEqual(WebSocketState.None, client.State);

        await Task.Delay(500, TestContext.CancellationTokenSource.Token);
        Assert.AreEqual(1, server.ConnectionCount, "主动断开不应触发自动重连");
    }

    [TestMethod]
    public async Task 异常断线按配置自动重连() {
        using WebSocketTestServer server = WebSocketTestServer.Start();
        WebSocketTestConfig config = new() {
            EndpointUrl = server.Uri.ToString(),
            AutoReconnect = true,
            ReconnectDelay = 100,
            MaxRetries = 5
        };
        using WebSocketProtocolClient client = new(config);

        TaskCompletionSource<WebSocketClosedEventArgs> closed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        client.Closed += (_, e) => closed.TrySetResult(e);
        TaskCompletionSource<bool> reconnected = new(TaskCreationOptions.RunContinuationsAsynchronously);
        client.Reconnected += (_, _) => reconnected.TrySetResult(true);

        await client.ConnectAsync(TestContext.CancellationTokenSource.Token);
        Assert.IsTrue(await TestWait.UntilAsync(() => server.ConnectionCount == 1));

        // 模拟异常断线：直接切断 TCP
        server.LastConnection!.KillTcp();

        WebSocketClosedEventArgs args = await closed.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationTokenSource.Token);
        Assert.IsTrue(args.IsAbnormal, "TCP 被切断应视为异常断线");

        Assert.IsTrue(await reconnected.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.CancellationTokenSource.Token), "应自动重连成功");
        Assert.AreEqual(2, server.ConnectionCount);
        Assert.IsTrue(await TestWait.UntilAsync(client.CheckConnection));

        // 重连后的连接应可以继续通信
        TaskCompletionSource<string?> echoed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        client.MessageReceived += (_, e) => echoed.TrySetResult(e.Text);
        await client.SendTextAsync("after-reconnect", TestContext.CancellationTokenSource.Token);
        Assert.AreEqual("after-reconnect", await echoed.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationTokenSource.Token));
    }

    [TestMethod]
    public async Task 关闭AutoReconnect时异常断线不重连() {
        using WebSocketTestServer server = WebSocketTestServer.Start();
        WebSocketTestConfig config = new() {
            EndpointUrl = server.Uri.ToString(),
            AutoReconnect = false,
            ReconnectDelay = 100,
            MaxRetries = 5
        };
        using WebSocketProtocolClient client = new(config);

        TaskCompletionSource<WebSocketClosedEventArgs> closed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        client.Closed += (_, e) => closed.TrySetResult(e);

        await client.ConnectAsync(TestContext.CancellationTokenSource.Token);
        Assert.IsTrue(await TestWait.UntilAsync(() => server.ConnectionCount == 1));

        server.LastConnection!.KillTcp();

        _ = await closed.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationTokenSource.Token);

        await Task.Delay(500, TestContext.CancellationTokenSource.Token);
        Assert.AreEqual(1, server.ConnectionCount, "AutoReconnect 关闭时不应重连");
        Assert.IsFalse(client.CheckConnection());
    }

    [TestMethod]
    public async Task 对端不确认关闭帧时超时仍触发Closed() {
        using WebSocketTestServer server = WebSocketTestServer.Start();
        // 空闲处理器不读取任何帧，因此永远不会确认客户端的关闭帧
        server.Handler = WebSocketTestServer.IdleHandlerAsync;

        WebSocketTestConfig config = new() { EndpointUrl = server.Uri.ToString(), CloseTimeout = 300 };
        using WebSocketProtocolClient client = new(config);

        TaskCompletionSource<WebSocketClosedEventArgs> closed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        client.Closed += (_, e) => closed.TrySetResult(e);

        await client.ConnectAsync(TestContext.CancellationTokenSource.Token);
        await client.DisconnectAsync(TestContext.CancellationTokenSource.Token);

        WebSocketClosedEventArgs args = await closed.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.CancellationTokenSource.Token);

        Assert.IsFalse(args.IsAbnormal, "主动关闭即使对端未确认也应视为正常关闭");
        Assert.AreEqual(WebSocketState.None, client.State);
    }

    [TestMethod]
    public async Task 连接过程中Dispose不产生释放竞态异常() {
        using WebSocketTestServer server = WebSocketTestServer.StartSilent();
        WebSocketTestConfig config = new() { EndpointUrl = server.Uri.ToString(), ConnectTimeout = 2000 };
        WebSocketProtocolClient client = new(config);

        Task connectTask = client.ConnectAsync(TestContext.CancellationTokenSource.Token);
        await Task.Delay(50, TestContext.CancellationTokenSource.Token);
        client.Dispose();

        try {
            await connectTask;
        } catch (Exception ex) {
            Assert.IsTrue(
                ex is ConnectionException or OperationCanceledException or ObjectDisposedException,
                $"连接与释放竞态只允许可预期的异常，实际为 {ex.GetType().Name}: {ex.Message}");
        }

        Assert.AreEqual(WebSocketState.None, client.State);
    }

    [TestMethod]
    public async Task 手动TryReconnectAsync可恢复连接() {
        using WebSocketTestServer server = WebSocketTestServer.Start();
        WebSocketTestConfig config = new() { EndpointUrl = server.Uri.ToString() };
        using WebSocketProtocolClient client = new(config);

        await client.ConnectAsync(TestContext.CancellationTokenSource.Token);
        await client.DisconnectAsync(TestContext.CancellationTokenSource.Token);
        Assert.IsFalse(client.CheckConnection());

        bool isReconnected = await client.TryReconnectAsync(maxAttempts: 3, retryDelay: 100, TestContext.CancellationTokenSource.Token);

        Assert.IsTrue(isReconnected);
        Assert.IsTrue(client.CheckConnection());
    }

    [TestMethod]
    public async Task 连接中Dispose后不再触发事件() {
        using WebSocketTestServer server = WebSocketTestServer.Start();
        WebSocketTestConfig config = new() {
            EndpointUrl = server.Uri.ToString(),
            AutoReconnect = true,
            ReconnectDelay = 100
        };
        WebSocketProtocolClient client = new(config);

        bool hasClosedFired = false;
        client.Closed += (_, _) => hasClosedFired = true;
        bool hasDisposedFired = false;
        client.Disposed += (_, _) => hasDisposedFired = true;

        await client.ConnectAsync(TestContext.CancellationTokenSource.Token);

        client.Dispose();
        client.Dispose();

        Assert.IsTrue(hasDisposedFired, "Dispose 应触发 Disposed 事件");
        Assert.AreEqual(WebSocketState.None, client.State);

        await Task.Delay(300, TestContext.CancellationTokenSource.Token);
        Assert.IsFalse(hasClosedFired, "释放后的内部清理不应再触发 Closed 事件");
        Assert.IsTrue(await TestWait.UntilAsync(() => server.ConnectionCount <= 1));
    }

    public TestContext TestContext { get; set; }
}

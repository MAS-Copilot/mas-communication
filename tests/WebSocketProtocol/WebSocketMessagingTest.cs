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
using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace MAS.CommunicationUnitTest.WebSocketProtocol;

/// <summary>
/// WebSocket 消息收发测试：文本、二进制、分片合并、并发发送、超时与大小限制（使用本地测试服务器）
/// </summary>
[TestClass]
public sealed class WebSocketMessagingTest {
    [TestMethod]
    public async Task 文本消息发送与接收() {
        using WebSocketTestServer server = WebSocketTestServer.Start();
        WebSocketTestConfig config = new() { EndpointUrl = server.Uri.ToString() };
        using WebSocketProtocolClient client = new(config);

        TaskCompletionSource<WebSocketMessageReceivedEventArgs> received = new(TaskCreationOptions.RunContinuationsAsynchronously);
        client.MessageReceived += (_, e) => received.TrySetResult(e);

        await client.ConnectAsync(TestContext.CancellationTokenSource.Token);
        await client.SendTextAsync("你好，MAS WebSocket！", TestContext.CancellationTokenSource.Token);

        WebSocketMessageReceivedEventArgs message = await received.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationTokenSource.Token);

        Assert.AreEqual(WebSocketMessageType.Text, message.MessageType);
        Assert.AreEqual("你好，MAS WebSocket！", message.Text);
    }

    [TestMethod]
    public async Task 二进制消息发送与接收() {
        using WebSocketTestServer server = WebSocketTestServer.Start();
        WebSocketTestConfig config = new() { EndpointUrl = server.Uri.ToString() };
        using WebSocketProtocolClient client = new(config);

        TaskCompletionSource<WebSocketMessageReceivedEventArgs> received = new(TaskCreationOptions.RunContinuationsAsynchronously);
        client.MessageReceived += (_, e) => received.TrySetResult(e);

        byte[] payload = new byte[4096];
        for (int i = 0; i < payload.Length; i++) {
            payload[i] = (byte)(i % 256);
        }

        await client.ConnectAsync(TestContext.CancellationTokenSource.Token);
        await client.SendBinaryAsync(payload, TestContext.CancellationTokenSource.Token);

        WebSocketMessageReceivedEventArgs message = await received.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationTokenSource.Token);

        Assert.AreEqual(WebSocketMessageType.Binary, message.MessageType);
        Assert.IsNull(message.Text);
        CollectionAssert.AreEqual(payload, message.Data);
    }

    [TestMethod]
    public async Task 分片消息正确合并() {
        string fullText = string.Concat(Enumerable.Range(0, 500).Select(i => $"part{i};"));

        using WebSocketTestServer server = WebSocketTestServer.Start();
        server.Handler = async (connection, token) => {
            await connection.SendFragmentedTextAsync(fullText, 7, token);
            await Task.Delay(Timeout.Infinite, token);
        };

        WebSocketTestConfig config = new() { EndpointUrl = server.Uri.ToString(), ReceiveBufferSize = 64 };
        using WebSocketProtocolClient client = new(config);

        TaskCompletionSource<WebSocketMessageReceivedEventArgs> received = new(TaskCreationOptions.RunContinuationsAsynchronously);
        client.MessageReceived += (_, e) => received.TrySetResult(e);

        await client.ConnectAsync(TestContext.CancellationTokenSource.Token);

        WebSocketMessageReceivedEventArgs message = await received.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.CancellationTokenSource.Token);

        Assert.AreEqual(WebSocketMessageType.Text, message.MessageType);
        Assert.AreEqual(fullText, message.Text, "分片消息应合并为一条完整消息");
    }

    [TestMethod]
    public async Task 并发发送不会交错() {
        const int MESSAGE_COUNT = 10;

        using WebSocketTestServer server = WebSocketTestServer.Start();
        WebSocketTestConfig config = new() { EndpointUrl = server.Uri.ToString(), WriteTimeout = 10000 };
        using WebSocketProtocolClient client = new(config);

        ConcurrentBag<string> echoed = [];
        client.MessageReceived += (_, e) => {
            if (e.Text is not null) {
                echoed.Add(e.Text);
            }
        };

        await client.ConnectAsync(TestContext.CancellationTokenSource.Token);

        List<string> sentMessages = [.. Enumerable.Range(0, MESSAGE_COUNT)
            .Select(i => $"msg-{i}|" + new string((char)('a' + i), 20000))];

        await Task.WhenAll(sentMessages.Select(m => Task.Run(() => client.SendTextAsync(m, TestContext.CancellationTokenSource.Token), TestContext.CancellationTokenSource.Token)));

        Assert.IsTrue(
            await TestWait.UntilAsync(() => echoed.Count >= MESSAGE_COUNT, 10000),
            $"应收到 {MESSAGE_COUNT} 条回显，实际 {echoed.Count} 条");

        CollectionAssert.AreEquivalent(sentMessages, echoed.ToList(), "并发发送的每条消息都应完整且不交错");
    }

    [TestMethod]
    public async Task 发送超时抛出WriteErrorException() {
        WebSocketTestConfig config = new() { EndpointUrl = "ws://127.0.0.1:18080/", WriteTimeout = 100 };
        using WebSocketProtocolClient client = new(config, () => new BlockingSendConnection());

        await client.ConnectAsync(TestContext.CancellationTokenSource.Token);

        _ = await Assert.ThrowsExactlyAsync<WriteErrorException>(
            () => client.SendBinaryAsync([1, 2, 3], TestContext.CancellationTokenSource.Token));
    }

    [TestMethod]
    public async Task 发送被调用方取消抛出OperationCanceledException() {
        WebSocketTestConfig config = new() { EndpointUrl = "ws://127.0.0.1:18080/", WriteTimeout = 30000 };
        using WebSocketProtocolClient client = new(config, () => new BlockingSendConnection());

        await client.ConnectAsync(TestContext.CancellationTokenSource.Token);

        using CancellationTokenSource cts = new(100);
        try {
            await client.SendBinaryAsync([1, 2, 3], cts.Token);
            Assert.Fail("调用方取消应抛出 OperationCanceledException 而不是 WriteErrorException");
        } catch (OperationCanceledException) {
            // 预期
        }
    }

    [TestMethod]
    public async Task 超大消息发送被拒绝() {
        using WebSocketTestServer server = WebSocketTestServer.Start();
        WebSocketTestConfig config = new() { EndpointUrl = server.Uri.ToString(), MaxMessageSize = 1024 };
        using WebSocketProtocolClient client = new(config);

        await client.ConnectAsync(TestContext.CancellationTokenSource.Token);

        _ = await Assert.ThrowsExactlyAsync<WriteErrorException>(() => client.SendBinaryAsync(new byte[2048], TestContext.CancellationTokenSource.Token));
        _ = await Assert.ThrowsExactlyAsync<WriteErrorException>(
            () => client.SendTextAsync(new string('x', 2048), TestContext.CancellationTokenSource.Token));

        Assert.AreEqual(WebSocketState.Open, client.State, "拒绝发生在发送前，连接不应受影响");
    }

    [TestMethod]
    public async Task 接收超大消息关闭连接() {
        using WebSocketTestServer server = WebSocketTestServer.Start();
        server.Handler = async (connection, token) => {
            await connection.SendBinaryAsync(new byte[8192], token);
            await Task.Delay(Timeout.Infinite, token);
        };

        WebSocketTestConfig config = new() { EndpointUrl = server.Uri.ToString(), MaxMessageSize = 1024 };
        using WebSocketProtocolClient client = new(config);

        TaskCompletionSource<WebSocketClosedEventArgs> closed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        client.Closed += (_, e) => closed.TrySetResult(e);
        bool hasReceivedMessage = false;
        client.MessageReceived += (_, _) => hasReceivedMessage = true;

        await client.ConnectAsync(TestContext.CancellationTokenSource.Token);

        WebSocketClosedEventArgs args = await closed.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationTokenSource.Token);

        Assert.IsTrue(args.IsAbnormal, "超限接收应视为异常关闭");
        Assert.IsFalse(hasReceivedMessage, "超限消息不应上抛给调用方");
        Assert.IsFalse(client.CheckConnection());
    }

    public TestContext TestContext { get; set; }

    /// <summary>
    /// 发送永久阻塞的假连接，用于确定性验证发送超时与取消行为
    /// </summary>
    private sealed class BlockingSendConnection : IWebSocketConnection {
        private WebSocketState _state = WebSocketState.None;

        public WebSocketState State => _state;
        public WebSocketCloseStatus? CloseStatus => null;
        public string? CloseStatusDescription => null;

        public Task ConnectAsync(Uri endpoint, CancellationToken token) {
            _state = WebSocketState.Open;
            return Task.CompletedTask;
        }

        public async Task SendAsync(byte[] payload, WebSocketMessageType messageType, CancellationToken token) {
            await Task.Delay(Timeout.Infinite, token);
        }

        public async Task<WebSocketFrame> ReceiveAsync(byte[] buffer, CancellationToken token) {
            await Task.Delay(Timeout.Infinite, token);
            return default;
        }

        public Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? description, CancellationToken token) {
            _state = WebSocketState.Closed;
            return Task.CompletedTask;
        }

        public void Abort() {
            _state = WebSocketState.Aborted;
        }

        public void Dispose() {
        }
    }
}

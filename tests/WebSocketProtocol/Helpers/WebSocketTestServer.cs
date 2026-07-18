// =============================================================================
// MAS.Communication
// https://www.mas-automation.com/
//
// Copyright 2026 MAS (厦门威光) Corporation
//
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.
// =============================================================================

using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;

namespace MAS.CommunicationUnitTest.WebSocketProtocol.Helpers;

/// <summary>
/// 记录一次 WebSocket 握手请求的信息，供测试断言使用
/// </summary>
internal sealed class WebSocketHandshakeInfo(
    string path,
    IReadOnlyDictionary<string, string> headers,
    IReadOnlyList<string> requestedSubProtocols) {
    public string Path { get; } = path;
    public IReadOnlyDictionary<string, string> Headers { get; } = headers;
    public IReadOnlyList<string> RequestedSubProtocols { get; } = requestedSubProtocols;
}

/// <summary>
/// 测试服务器接受的一个 WebSocket 连接
/// </summary>
internal sealed class WebSocketTestConnection(TcpClient tcp, WebSocket socket, WebSocketHandshakeInfo handshake) : IDisposable {
    private readonly TcpClient _tcp = tcp;

    public WebSocket Socket { get; } = socket;
    public WebSocketHandshakeInfo Handshake { get; } = handshake;
    public ConcurrentQueue<(WebSocketMessageType Type, byte[] Data)> ReceivedMessages { get; } = new();

    /// <summary>
    /// 直接切断 TCP 连接（发送 RST），模拟异常断线
    /// </summary>
    public void KillTcp() {
        try {
            _tcp.Client.LingerState = new LingerOption(true, 0);
        } catch {
            // Socket 可能已关闭
        }

        try {
            _tcp.Client.Close();
        } catch {
            // Socket 可能已关闭
        }
    }

    public Task SendTextAsync(string text, CancellationToken token = default) {
        byte[] payload = Encoding.UTF8.GetBytes(text);
        return Socket.SendAsync(new ArraySegment<byte>(payload), WebSocketMessageType.Text, true, token);
    }

    public Task SendBinaryAsync(byte[] data, CancellationToken token = default) {
        return Socket.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Binary, true, token);
    }

    /// <summary>
    /// 将一条文本消息拆分为多个帧发送，用于验证客户端的分片合并
    /// </summary>
    public async Task SendFragmentedTextAsync(string text, int fragmentSize, CancellationToken token = default) {
        byte[] payload = Encoding.UTF8.GetBytes(text);
        for (int offset = 0; offset < payload.Length; offset += fragmentSize) {
            int count = Math.Min(fragmentSize, payload.Length - offset);
            bool isLast = offset + count >= payload.Length;
            await Socket.SendAsync(
                new ArraySegment<byte>(payload, offset, count),
                WebSocketMessageType.Text,
                isLast,
                token);
        }
    }

    public void Dispose() {
        try {
            Socket.Dispose();
        } catch {
            // 连接可能已被测试场景终止
        }

        try {
            _tcp.Close();
        } catch {
            // 连接可能已被测试场景终止
        }
    }
}

/// <summary>
/// 基于 <see cref="TcpListener"/> 与 <see cref="WebSocket.CreateFromStream(Stream, bool, string?, TimeSpan)"/>
/// 的本地 WebSocket 测试服务器，不依赖公网与第三方组件
/// </summary>
internal sealed class WebSocketTestServer : IDisposable {
    private const string WEBSOCKET_ACCEPT_GUID = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

    private readonly TcpListener _listener;
    private readonly CancellationTokenSource _cts = new();
    private readonly ConcurrentQueue<WebSocketTestConnection> _connections = new();
    private readonly bool _completeHandshake;
    private int _connectionCount;
    private Task? _acceptLoop;
    private bool _disposed;

    private WebSocketTestServer(bool completeHandshake) {
        _completeHandshake = completeHandshake;
        _listener = new TcpListener(IPAddress.Loopback, 0);
        _listener.Start();
        int port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        Uri = new Uri($"ws://127.0.0.1:{port}/");
    }

    /// <summary>
    /// 获取服务器的 ws:// 地址
    /// </summary>
    public Uri Uri { get; }

    /// <summary>
    /// 获取或设置连接处理器，默认为回显
    /// </summary>
    public Func<WebSocketTestConnection, CancellationToken, Task> Handler { get; set; } = EchoHandlerAsync;

    /// <summary>
    /// 获取已完成握手的连接总数
    /// </summary>
    public int ConnectionCount => Volatile.Read(ref _connectionCount);

    /// <summary>
    /// 获取最近一个完成握手的连接
    /// </summary>
    public WebSocketTestConnection? LastConnection => _connections.ToArray().LastOrDefault();

    /// <summary>
    /// 启动一个正常完成 WebSocket 握手的测试服务器
    /// </summary>
    public static WebSocketTestServer Start() {
        WebSocketTestServer server = new(completeHandshake: true);
        server._acceptLoop = Task.Run(server.AcceptLoopAsync);
        return server;
    }

    /// <summary>
    /// 启动一个只接受 TCP 但永不完成握手的服务器，用于测试连接取消与超时
    /// </summary>
    public static WebSocketTestServer StartSilent() {
        WebSocketTestServer server = new(completeHandshake: false);
        server._acceptLoop = Task.Run(server.AcceptLoopAsync);
        return server;
    }

    /// <summary>
    /// 回显处理器：完整接收一条消息后原样发回
    /// </summary>
    public static async Task EchoHandlerAsync(WebSocketTestConnection connection, CancellationToken token) {
        WebSocket socket = connection.Socket;
        byte[] buffer = new byte[64 * 1024];
        using MemoryStream message = new();

        while (socket.State == WebSocketState.Open) {
            WebSocketReceiveResult result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
            if (result.MessageType == WebSocketMessageType.Close) {
                await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, token);
                return;
            }

            message.Write(buffer, 0, result.Count);
            if (!result.EndOfMessage) {
                continue;
            }

            byte[] payload = message.ToArray();
            message.SetLength(0);
            connection.ReceivedMessages.Enqueue((result.MessageType, payload));
            await socket.SendAsync(new ArraySegment<byte>(payload), result.MessageType, true, token);
        }
    }

    /// <summary>
    /// 空闲处理器：不读也不写，用于制造发送阻塞
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:删除未使用的参数", Justification = "<挂起>")]
    public static async Task IdleHandlerAsync(WebSocketTestConnection connection, CancellationToken token) {
        await Task.Delay(Timeout.Infinite, token);
    }

    public void Dispose() {
        if (_disposed) {
            return;
        }

        _disposed = true;
        _cts.Cancel();

        try {
            _listener.Stop();
        } catch {
            // 监听器可能已停止
        }

        foreach (WebSocketTestConnection connection in _connections) {
            connection.Dispose();
        }

        try {
            _ = _acceptLoop?.Wait(1000);
        } catch {
            // 关闭阶段忽略接收循环异常
        }

        _cts.Dispose();
    }

    private async Task AcceptLoopAsync() {
        try {
            while (!_cts.IsCancellationRequested) {
                TcpClient tcp = await _listener.AcceptTcpClientAsync(_cts.Token);
                _ = Task.Run(() => HandleClientAsync(tcp));
            }
        } catch (OperationCanceledException) {
            // 服务器停止
        } catch (SocketException) {
            // 服务器停止
        } catch (ObjectDisposedException) {
            // 服务器停止
        }
    }

    private async Task HandleClientAsync(TcpClient tcp) {
        try {
            if (!_completeHandshake) {
                await Task.Delay(Timeout.Infinite, _cts.Token);
                return;
            }

            await UpgradeAndHandleAsync(tcp);
        } catch {
            // 连接被测试场景终止或服务器停止
        } finally {
            try {
                tcp.Close();
            } catch {
                // 连接可能已关闭
            }
        }
    }

    private async Task UpgradeAndHandleAsync(TcpClient tcp) {
        NetworkStream stream = tcp.GetStream();
        WebSocketHandshakeInfo handshake = await ReadHandshakeAsync(stream, _cts.Token);
        string? subProtocol = handshake.RequestedSubProtocols.Count > 0 ? handshake.RequestedSubProtocols[0] : null;
        await WriteHandshakeResponseAsync(stream, handshake, subProtocol, _cts.Token);

        WebSocket socket = WebSocket.CreateFromStream(stream, isServer: true, subProtocol, TimeSpan.FromSeconds(30));
        WebSocketTestConnection connection = new(tcp, socket, handshake);
        _connections.Enqueue(connection);
        _ = Interlocked.Increment(ref _connectionCount);

        await Handler(connection, _cts.Token);
    }

    private static async Task<WebSocketHandshakeInfo> ReadHandshakeAsync(NetworkStream stream, CancellationToken token) {
        List<byte> request = [];
        byte[] single = new byte[1];

        while (!EndsWithDoubleCrlf(request)) {
            int read = await stream.ReadAsync(single.AsMemory(0, 1), token);
            if (read == 0) {
                throw new IOException("连接在握手完成前被关闭");
            }

            request.Add(single[0]);
            if (request.Count > 64 * 1024) {
                throw new InvalidOperationException("握手请求过大");
            }
        }

        return ParseHandshake(Encoding.ASCII.GetString([.. request]));
    }

    private static bool EndsWithDoubleCrlf(List<byte> bytes) {
        int count = bytes.Count;
        return count >= 4 &&
               bytes[count - 4] == (byte)'\r' && bytes[count - 3] == (byte)'\n' &&
               bytes[count - 2] == (byte)'\r' && bytes[count - 1] == (byte)'\n';
    }

    private static WebSocketHandshakeInfo ParseHandshake(string request) {
        string[] lines = request.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        string[] requestLine = lines[0].Split(' ');
        string path = requestLine.Length > 1 ? requestLine[1] : "/";

        Dictionary<string, string> headers = new(StringComparer.OrdinalIgnoreCase);
        foreach (string line in lines.Skip(1)) {
            int separator = line.IndexOf(':');
            if (separator <= 0) {
                continue;
            }

            string key = line[..separator].Trim();
            string value = line[(separator + 1)..].Trim();
            headers[key] = headers.TryGetValue(key, out string? existing) ? $"{existing}, {value}" : value;
        }

        List<string> subProtocols = [];
        if (headers.TryGetValue("Sec-WebSocket-Protocol", out string? rawProtocols)) {
            subProtocols = [.. rawProtocols.Split(',').Select(p => p.Trim()).Where(p => p.Length > 0)];
        }

        return new WebSocketHandshakeInfo(path, headers, subProtocols);
    }

    private static async Task WriteHandshakeResponseAsync(
        NetworkStream stream,
        WebSocketHandshakeInfo handshake,
        string? subProtocol,
        CancellationToken token) {
        string key = handshake.Headers["Sec-WebSocket-Key"];
        string accept = Convert.ToBase64String(SHA1.HashData(Encoding.ASCII.GetBytes(key + WEBSOCKET_ACCEPT_GUID)));

        StringBuilder response = new();
        _ = response.Append("HTTP/1.1 101 Switching Protocols\r\n");
        _ = response.Append("Connection: Upgrade\r\n");
        _ = response.Append("Upgrade: websocket\r\n");
        _ = response.Append($"Sec-WebSocket-Accept: {accept}\r\n");
        if (subProtocol is not null) {
            _ = response.Append($"Sec-WebSocket-Protocol: {subProtocol}\r\n");
        }

        _ = response.Append("\r\n");

        byte[] payload = Encoding.ASCII.GetBytes(response.ToString());
        await stream.WriteAsync(payload, token);
        await stream.FlushAsync(token);
    }
}

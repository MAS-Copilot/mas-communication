// =============================================================================
// MAS.Communication
// https://www.mas-automation.com/
//
// Copyright 2026 MAS (厦门威光) Corporation
//
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.
// =============================================================================

using System.Net.WebSockets;
#if !NETFRAMEWORK && !NET9_0_OR_GREATER
using System.Net.Http;
using System.Net.Sockets;
#endif

namespace MAS.Communication.WebSocketProtocol;

internal sealed class ClientWebSocketConnection : IWebSocketConnection {
    private readonly ClientWebSocket _socket;
    private readonly IWebSocketCommunicationConfig _config;
#if !NETFRAMEWORK && !NET9_0_OR_GREATER
    private HttpMessageInvoker? _invoker;
#endif

    public ClientWebSocketConnection(IWebSocketCommunicationConfig config) {
        _config = config;
        _socket = new ClientWebSocket();

        if (!string.IsNullOrEmpty(config.SubProtocol)) {
            _socket.Options.AddSubProtocol(config.SubProtocol);
        }

        _socket.Options.KeepAliveInterval = config.KeepAliveInterval > 0
            ? TimeSpan.FromMilliseconds(config.KeepAliveInterval)
            : TimeSpan.Zero;

#if NET9_0_OR_GREATER
        bool canUsePingTimeout = config.KeepAliveTimeout > 0 && config.KeepAliveInterval > 0;
        if (canUsePingTimeout) {
            // KeepAliveInterval 作为 Ping 间隔，超时未收到 Pong 即中止连接，由接收循环感知断线
            _socket.Options.KeepAliveTimeout = TimeSpan.FromMilliseconds(config.KeepAliveTimeout);
        }
#endif

        ApplyCustomHeaders(config);
    }

    public WebSocketState State => _socket.State;

    public WebSocketCloseStatus? CloseStatus => _socket.CloseStatus;

    public string? CloseStatusDescription => _socket.CloseStatusDescription;

    public async Task ConnectAsync(Uri endpoint, CancellationToken token) {
#if !NETFRAMEWORK && !NET9_0_OR_GREATER
        bool canUseTcpKeepAlive = _config.KeepAliveTimeout > 0 && _config.KeepAliveInterval > 0;
        if (canUseTcpKeepAlive) {
            _invoker = CreateTcpKeepAliveInvoker(_config);
            await _socket.ConnectAsync(endpoint, _invoker, token).ConfigureAwait(false);
            return;
        }
#endif
        await _socket.ConnectAsync(endpoint, token).ConfigureAwait(false);
    }

    public async Task SendAsync(byte[] payload, WebSocketMessageType messageType, CancellationToken token) {
#if !NETFRAMEWORK
        await _socket.SendAsync(payload.AsMemory(), messageType, true, token).ConfigureAwait(false);
#else
        await _socket.SendAsync(new ArraySegment<byte>(payload), messageType, true, token).ConfigureAwait(false);
#endif
    }

    public async Task<WebSocketFrame> ReceiveAsync(byte[] buffer, CancellationToken token) {
#if !NETFRAMEWORK
        ValueWebSocketReceiveResult result = await _socket.ReceiveAsync(buffer.AsMemory(), token).ConfigureAwait(false);
#else
        WebSocketReceiveResult result = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), token).ConfigureAwait(false);
#endif
        return new WebSocketFrame(result.Count, result.MessageType, result.EndOfMessage);
    }

    public Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? description, CancellationToken token) {
        return _socket.CloseOutputAsync(closeStatus, description, token);
    }

    public void Abort() {
        _socket.Abort();
    }

    public void Dispose() {
        _socket.Dispose();
#if !NETFRAMEWORK && !NET9_0_OR_GREATER
        _invoker?.Dispose();
        _invoker = null;
#endif
    }

    private void ApplyCustomHeaders(IWebSocketCommunicationConfig config) {
        if (config.Headers is null) {
            return;
        }

        foreach (KeyValuePair<string, string> header in config.Headers) {
            _socket.Options.SetRequestHeader(header.Key, header.Value);
        }
    }

#if !NETFRAMEWORK && !NET9_0_OR_GREATER
    private static HttpMessageInvoker CreateTcpKeepAliveInvoker(IWebSocketCommunicationConfig config) {
        int keepAliveTimeSeconds = Math.Max(1, config.KeepAliveInterval / 1000);
        int keepAliveIntervalSeconds = Math.Max(1, config.KeepAliveTimeout / 3000);

        SocketsHttpHandler handler = new() {
            ConnectCallback = async (context, token) => {
                Socket socket = new(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, keepAliveTimeSeconds);
                socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, keepAliveIntervalSeconds);
                socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount, 3);

                try {
                    await socket.ConnectAsync(context.DnsEndPoint, token).ConfigureAwait(false);
                    return new NetworkStream(socket, ownsSocket: true);
                } catch {
                    socket.Dispose();
                    throw;
                }
            }
        };

        return new HttpMessageInvoker(handler, disposeHandler: true);
    }
#endif
}

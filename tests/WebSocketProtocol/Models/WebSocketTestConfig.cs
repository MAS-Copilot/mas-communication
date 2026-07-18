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

namespace MAS.CommunicationUnitTest.WebSocketProtocol.Models;

/// <summary>
/// 用于测试的 WebSocket 配置实现，同时演示调用方应如何实现 <see cref="IWebSocketCommunicationConfig"/> 与 <c>Clone</c>
/// </summary>
public sealed class WebSocketTestConfig : IWebSocketCommunicationConfig {
    public string ProtocolName => "WebSocket";

    public string Ip { get; set; } = string.Empty;
    public int MaxRetries { get; set; } = 3;
    public int ReadTimeout { get; set; } = 5000;
    public int WriteTimeout { get; set; } = 5000;

    public string EndpointUrl { get; set; } = "ws://127.0.0.1:0/";
    public string? SubProtocol { get; set; }
    public string? ConnectionIdentity { get; set; }
    public IReadOnlyDictionary<string, string>? Headers { get; set; }

    public int ConnectTimeout { get; set; } = 5000;
    public int KeepAliveInterval { get; set; }
    public int KeepAliveTimeout { get; set; }
    public bool AutoReconnect { get; set; }
    public int ReconnectDelay { get; set; } = 100;
    public int MaxMessageSize { get; set; } = 16 * 1024 * 1024;
    public int ReceiveBufferSize { get; set; } = 16 * 1024;
    public int CloseTimeout { get; set; } = 2000;

    public T Clone<T>() where T : ICommunicationConfig {
        return (T)Clone();
    }

    public object Clone() {
        return new WebSocketTestConfig {
            Ip = Ip,
            MaxRetries = MaxRetries,
            ReadTimeout = ReadTimeout,
            WriteTimeout = WriteTimeout,
            EndpointUrl = EndpointUrl,
            SubProtocol = SubProtocol,
            ConnectionIdentity = ConnectionIdentity,
            Headers = Headers,
            ConnectTimeout = ConnectTimeout,
            KeepAliveInterval = KeepAliveInterval,
            KeepAliveTimeout = KeepAliveTimeout,
            AutoReconnect = AutoReconnect,
            ReconnectDelay = ReconnectDelay,
            MaxMessageSize = MaxMessageSize,
            ReceiveBufferSize = ReceiveBufferSize,
            CloseTimeout = CloseTimeout
        };
    }
}

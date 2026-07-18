// =============================================================================
// MAS.Communication
// https://www.mas-automation.com/
//
// Copyright 2026 MAS (厦门威光) Corporation
//
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.
// =============================================================================

namespace MAS.Communication.WebSocketProtocol;

/// <summary>
/// WebSocket 通信配置接口
/// </summary>
/// <remarks>
/// <para>
/// <see cref="EndpointUrl"/> 是连接主键（例如 <c>ws://192.168.1.10:8080/path</c> 或
/// <c>wss://gateway.example.com/ws</c>），仅接受 <c>ws://</c> 与 <c>wss://</c> 方案。
/// 基类继承的 <see cref="ICommunicationConfig.Ip"/> 仅为兼容现有框架，不参与连接与实例键计算。
/// </para>
/// <para>
/// <see cref="Headers"/> 用于承载调用方提供的自定义请求头（例如 <c>Authorization: Bearer xxx</c>）。
/// 出于安全考虑，请求头不参与实例键计算，也不会进入日志或异常信息。
/// </para>
/// <para>
/// 配置会在客户端创建时克隆为快照。请确保 <c>Clone</c> 实现完整复制以下所有字段
/// （包括 <see cref="Headers"/> 引用），否则可能导致实例无法从管理器缓存中正确移除。
/// </para>
/// <para>
/// 基类的 <see cref="ICommunicationConfig.MaxRetries"/> 在本协议中作为自动重连的最大尝试次数；
/// <see cref="ICommunicationConfig.WriteTimeout"/> 作为单次发送的超时时间。
/// </para>
/// </remarks>
public interface IWebSocketCommunicationConfig : ICommunicationConfig {
    /// <summary>
    /// 获取或设置端点地址，是 WebSocket 的连接主键
    /// </summary>
    /// <remarks>仅接受 <c>ws://</c> 与 <c>wss://</c>，例如 <c>wss://gateway.example.com/ws</c></remarks>
    string EndpointUrl { get; set; }

    /// <summary>
    /// 获取或设置 WebSocket 子协议（Sec-WebSocket-Protocol）
    /// </summary>
    /// <remarks>为 <see langword="null"/> 或空字符串时不携带子协议；参与实例键计算</remarks>
    string? SubProtocol { get; set; }

    /// <summary>
    /// 获取或设置非敏感的连接身份标识，参与实例键计算
    /// </summary>
    /// <remarks>
    /// 当多个调用方使用不同认证身份（不同 Token、不同账号）连接同一端点时，
    /// 必须设置不同的身份标识（例如用户名、租户名或客户端编号），否则会错误复用同一连接。
    /// 不要将 Token、密码等敏感内容作为身份标识。
    /// </remarks>
    string? ConnectionIdentity { get; set; }

    /// <summary>
    /// 获取调用方提供的自定义请求头（例如 Bearer Token 等认证头）
    /// </summary>
    /// <remarks>不参与实例键计算；键为请求头名称，值为请求头内容</remarks>
    IReadOnlyDictionary<string, string>? Headers { get; }

    /// <summary>
    /// 获取或设置建立连接的超时时间（毫秒）
    /// </summary>
    /// <remarks>小于等于 0 表示不限制</remarks>
    int ConnectTimeout { get; set; }

    /// <summary>
    /// 获取或设置心跳保活间隔（毫秒）
    /// </summary>
    /// <remarks>基于 WebSocket 协议层的保活帧实现；小于等于 0 表示关闭心跳</remarks>
    int KeepAliveInterval { get; set; }

    /// <summary>
    /// 获取或设置心跳应答超时时间（毫秒），用于发现网络黑洞等 TCP 尚未报错的死连接
    /// </summary>
    /// <remarks>
    /// <para>小于等于 0 表示关闭；需要同时启用 <see cref="KeepAliveInterval"/>。</para>
    /// <para>各目标框架的实现：<c>net9.0</c> 及以上使用协议层 Ping/Pong 应答超时；
    /// <c>net8.0</c> 使用 TCP Keep-Alive 探测（秒级近似）；
    /// <c>net481</c> 无法在传输层按连接强制，建议由服务端周期性下发 Ping 或在应用层实现心跳。</para>
    /// </remarks>
    int KeepAliveTimeout { get; set; }

    /// <summary>
    /// 获取或设置一个值，指示异常断线后是否自动重连
    /// </summary>
    /// <remarks>仅在异常断线时生效；主动断开与远端正常关闭不会触发自动重连</remarks>
    bool AutoReconnect { get; set; }

    /// <summary>
    /// 获取或设置自动重连的等待时间（毫秒）
    /// </summary>
    int ReconnectDelay { get; set; }

    /// <summary>
    /// 获取或设置单条消息的最大字节数（收发共用）
    /// </summary>
    /// <remarks>发送超过上限的消息会被拒绝；接收超过上限的消息会以 MessageTooBig 关闭连接</remarks>
    int MaxMessageSize { get; set; }

    /// <summary>
    /// 获取或设置接收缓冲区大小（字节）
    /// </summary>
    int ReceiveBufferSize { get; set; }

    /// <summary>
    /// 获取或设置优雅关闭的超时时间（毫秒）
    /// </summary>
    /// <remarks>关闭握手超过该时间后将直接中止连接</remarks>
    int CloseTimeout { get; set; }
}

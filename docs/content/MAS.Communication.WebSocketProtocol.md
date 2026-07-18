# MAS.Communication.WebSocketProtocol

## 通信配置

调用方需实现 `IWebSocketCommunicationConfig`（继承自 `ICommunicationConfig`）。

| 字段 | 说明 |
| ---- | ---- |
| `EndpointUrl` | 连接主键，仅接受 `ws://` 与 `wss://`（拒绝 `http://` 与 `https://`） |
| `SubProtocol` | WebSocket 子协议（Sec-WebSocket-Protocol），参与实例键计算 |
| `ConnectionIdentity` | 非敏感的连接身份标识（用户名、租户名、客户端编号等），参与实例键计算 |
| `Headers` | 调用方提供的自定义请求头（例如 `Authorization: Bearer xxx`），不参与实例键 |
| `ConnectTimeout` | 建立连接的超时时间（毫秒） |
| `KeepAliveInterval` | 心跳保活间隔（毫秒），基于协议层保活帧；小于等于 0 关闭心跳 |
| `KeepAliveTimeout` | 心跳应答超时（毫秒），用于发现网络黑洞等 TCP 尚未报错的死连接；需配合 `KeepAliveInterval` |
| `AutoReconnect` / `ReconnectDelay` | 异常断线后是否自动重连及重连等待时间；基类 `MaxRetries` 作为最大尝试次数 |
| `MaxMessageSize` | 单条消息最大字节数（收发共用） |
| `ReceiveBufferSize` | 接收缓冲区大小（字节） |
| `CloseTimeout` | 优雅关闭握手的超时时间（毫秒），超时后直接中止连接并统一发布 `Closed` |

> **兼容性说明**：基类继承的 `Ip` 字段仅为兼容现有框架保留，WebSocket 不使用它建立连接，也不参与实例键计算；连接完全由 `EndpointUrl` 决定。
>
> **实例键规则**：实例键 = 规范化后的 `EndpointUrl` + `SubProtocol` + `ConnectionIdentity`。
> 规范化只作用于 scheme、host 的大小写与默认端口（`ws` 的 80、`wss` 的 443）；
> path 与 query 的大小写、结尾斜杠可能有语义，会原样保留。
>
> **身份隔离**：当多个调用方使用不同认证身份（不同 Token、不同账号）连接同一端点时，
> 必须设置不同的 `ConnectionIdentity`，否则会复用同一连接造成身份串用
> Token、Cookie 等敏感请求头永远不会进入实例键、日志或异常信息，因此不能依赖它们区分实例
>
> **心跳应答超时（按目标框架）**：`net9.0`/`net10.0` 使用协议层 Ping/Pong 应答超时（`KeepAliveInterval` 为 Ping 间隔，`KeepAliveTimeout` 内未收到 Pong 即中止连接并触发自动重连）；`net8.0` 通过 TCP Keep-Alive 探测实现秒级近似检测；`net481` 无法在传输层按连接强制，建议由服务端周期性下发 Ping 或在应用层实现心跳
>
> **TLS**：`wss://` 使用系统默认的 TLS 证书校验策略，不提供跳过校验的选项

## 使用示例

以下示例演示如何通过 `IProtocolManager` 获取 `IWebSocketProtocol` 实例，并完成连接、收发消息、订阅事件与断开

```csharp
using MAS.Communication;
using MAS.Communication.WebSocketProtocol;

// 一个最小的配置实现
public sealed class WebSocketConfig : IWebSocketCommunicationConfig {
    public string ProtocolName => "WebSocket";

    // Ip 仅为兼容基础接口保留，不参与连接
    public string Ip { get; set; } = string.Empty;
    public int MaxRetries { get; set; } = 3;
    public int ReadTimeout { get; set; } = 5000;
    public int WriteTimeout { get; set; } = 5000;

    public string EndpointUrl { get; set; } = "wss://gateway.example.com/ws";
    public string? SubProtocol { get; set; }
    public string? ConnectionIdentity { get; set; }
    public IReadOnlyDictionary<string, string>? Headers { get; set; }

    public int ConnectTimeout { get; set; } = 10000;
    public int KeepAliveInterval { get; set; } = 30000;
    public int KeepAliveTimeout { get; set; } = 10000;
    public bool AutoReconnect { get; set; } = true;
    public int ReconnectDelay { get; set; } = 1000;
    public int MaxMessageSize { get; set; } = 16 * 1024 * 1024;
    public int ReceiveBufferSize { get; set; } = 16 * 1024;
    public int CloseTimeout { get; set; } = 3000;

    public T Clone<T>() where T : ICommunicationConfig => (T)Clone();

    public object Clone() => new WebSocketConfig {
        Ip = Ip, MaxRetries = MaxRetries, ReadTimeout = ReadTimeout, WriteTimeout = WriteTimeout,
        EndpointUrl = EndpointUrl, SubProtocol = SubProtocol, ConnectionIdentity = ConnectionIdentity,
        Headers = Headers, ConnectTimeout = ConnectTimeout, KeepAliveInterval = KeepAliveInterval,
        KeepAliveTimeout = KeepAliveTimeout, AutoReconnect = AutoReconnect, ReconnectDelay = ReconnectDelay,
        MaxMessageSize = MaxMessageSize, ReceiveBufferSize = ReceiveBufferSize,
        CloseTimeout = CloseTimeout
    };
}

public sealed class GatewayService {
    private readonly IProtocolManager _protocolManager;

    public GatewayService(IProtocolManager protocolManager) {
        _protocolManager = protocolManager;
    }

    public async Task RunAsync() {
        WebSocketConfig config = new() {
            EndpointUrl = "wss://gateway.example.com/ws",
            SubProtocol = "mas.v1",
            // 不同认证身份必须设置不同的身份标识，避免复用同一连接造成身份串用
            ConnectionIdentity = "tenant-a",
            Headers = new Dictionary<string, string> {
                // 认证头由调用方提供；Token 刷新等属于应用层
                ["Authorization"] = "Bearer <your-token>"
            }
        };

        // 1. 获取强类型协议实例（相同 EndpointUrl + SubProtocol 会复用实例）
        IWebSocketProtocol ws = _protocolManager.GetOrCreate<IWebSocketProtocol>(config);

        // 2. 订阅事件（分片消息在内部合并完成后才触发一次）
        ws.MessageReceived += (_, e) => {
            if (e.Text is not null) {
                Console.WriteLine($"收到文本：{e.Text}");
            } else {
                Console.WriteLine($"收到二进制：{e.Data.Length} 字节");
            }
        };

        ws.Closed += (_, e) => {
            Console.WriteLine(e.IsAbnormal
                ? $"异常断线：{e.Exception!.Message}"
                : $"连接关闭：{e.CloseStatus}");
        };

        ws.Reconnected += (_, _) => Console.WriteLine("自动重连成功");

        // 3. 建立连接（启动后台接收循环与心跳）
        await ws.ConnectAsync();

        // 4. 发送文本与二进制消息（并发调用会自动串行化，不会交错）
        await ws.SendTextAsync("""{"type":"hello"}""");
        await ws.SendBinaryAsync([0x01, 0x02, 0x03]);

        // 5. 正常关闭握手并断开（主动断开不会触发自动重连）
        await ws.DisconnectAsync();
    }
}
```

## 生命周期语义

| 方法 | WebSocket 行为 |
| ---- | ---- |
| `ConnectAsync` | 校验 `ws://`/`wss://` 方案、携带子协议与自定义请求头完成握手、启动后台接收循环与协议层心跳 |
| `CheckConnection` | 当前连接状态为 `Open` |
| `ProbeConnectionAsync` | 建立临时连接完成握手后立即关闭，不影响当前主连接 |
| `TryReconnectAsync` | 串行执行重建连接，成功返回 `true` |
| `DisconnectAsync` / `Disconnect` | 发送关闭帧并等待对端确认（超时则中止）；无论对端是否确认，都会恰好发布一次 `Closed`；不触发自动重连，对象仍可再次连接 |
| `Dispose` | 停止重连与接收循环、中止连接并释放资源，触发 `Disposed` 事件供管理器移除缓存 |

**断线检测与自动重连**：后台接收循环持续监视连接；异常断线（网络错误、对端崩溃等）触发 `Closed`（`IsAbnormal == true`），当 `AutoReconnect` 开启时按 `ReconnectDelay` 间隔自动重连（最多 `MaxRetries` 次），成功后触发 `Reconnected`。远端正常关闭与主动断开只触发 `Closed`，不会自动重连。对于 TCP 尚未报错的网络黑洞（如 NAT 映射失效），需启用 `KeepAliveTimeout`（详见上文按目标框架的说明），超时未获应答的连接会被中止并进入相同的断线检测与重连流程。

## 异常与返回策略

- 建立连接失败、方案不合法（`http://`/`https://`）、连接超时：`ConnectionException`
- 连接未建立时发送：`ConnectionException`
- 发送失败、发送超时（`WriteTimeout`）、消息超过 `MaxMessageSize`：`WriteErrorException`
- 取消：原样抛出 `OperationCanceledException`
- 接收消息超过 `MaxMessageSize`：以 `MessageTooBig` 关闭连接并触发 `Closed`（`IsAbnormal == true`），超限消息不会上抛给调用方

> `net481` 兼容性：内部只使用 `ArraySegment<byte>` 重载与两端一致的 Close API，公共接口不暴露 `Memory<byte>` 等在 .NET Framework 上有额外兼容负担的类型。

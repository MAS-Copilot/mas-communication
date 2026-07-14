# MAS.Communication.OpcUaProtocol

OPC UA 复用统一的 `IProtocol` 生命周期与 `IProtocolManager` 实例管理，但保持自己的“节点、会话、订阅、安全”模型，不会被抽象成地址读写接口。业务能力通过专属接口 `IOpcUaProtocol` 暴露，公共 API 只使用库自有的轻量模型（如 `OpcUaNodeId`、`OpcUaValue`），不会泄漏官方 SDK 类型。

> 底层基于 OPC Foundation 官方客户端 [UA-.NETStandard](https://github.com/OPCFoundation/UA-.NETStandard)，`Core`/`Client`/`Configuration` 三个包锁定统一版本。

## 通信配置

调用方需实现 `IOpcUaCommunicationConfig`（继承自 `ICommunicationConfig`）。

| 字段 | 说明 |
| ---- | ---- |
| `EndpointUrl` | 连接主键，例如 `opc.tcp://192.168.1.10:4840/Server` |
| `ApplicationName` / `ApplicationUri` | 客户端应用名称与 URI |
| `SecurityMode` / `SecurityPolicyUri` | 参与端点选择的安全模式与安全策略 |
| `IdentityType` / `UserName` / `CredentialKey` / `ClientCertificateThumbprint` | 身份类型及对应参数 |
| `CredentialProvider` | 凭据提供者；用于按 `CredentialKey` 解析密码，避免密码进入配置 |
| `CertificateStorePath` | PKI 证书存储根路径 |
| `AutoAcceptUntrustedCertificates` | 是否自动接受不受信任的服务端证书（默认 `false`） |
| `SessionTimeout` / `KeepAliveInterval` / `ReconnectDelay` | 会话与保活/重连参数 |
| `UseEndpointDiscovery` | 是否在找不到精确匹配端点时回退到端点发现 |

> **安全提示**：密码不会进入实例键、日志或异常信息。推荐通过 `CredentialKey` + `IOpcUaCredentialProvider` 在建立会话时按需解析。`AutoAcceptUntrustedCertificates` 默认必须为 `false`，仅建议在受控开发环境临时开启。
>
> 配置会在客户端创建时被克隆为快照。请确保 `Clone` 实现完整复制所有字段（包括 `CredentialProvider` 引用），否则实例可能无法从管理器缓存中正确移除。

## 使用示例

以下示例演示如何通过 `IProtocolManager` 获取 `IOpcUaProtocol` 实例，并完成连接、读写、浏览、方法调用与订阅。

```csharp
using MAS.Communication;
using MAS.Communication.OpcUaProtocol;

// 一个最小的配置实现
public sealed class OpcUaConfig : IOpcUaCommunicationConfig {
    public string ProtocolName => "OpcUa";

    public string Ip { get; set; } = "192.168.1.10";
    public int MaxRetries { get; set; } = 3;
    public int ReadTimeout { get; set; } = 5000;
    public int WriteTimeout { get; set; } = 5000;

    public string EndpointUrl { get; set; } = "opc.tcp://192.168.1.10:4840/Server";
    public string ApplicationName { get; set; } = "MAS.Communication.Client";
    public string ApplicationUri { get; set; } = "urn:localhost:MAS:Communication:Client";

    public OpcUaSecurityMode SecurityMode { get; set; } = OpcUaSecurityMode.SignAndEncrypt;
    public string SecurityPolicyUri { get; set; } = "http://opcfoundation.org/UA/SecurityPolicy#Basic256Sha256";

    public OpcUaIdentityType IdentityType { get; set; } = OpcUaIdentityType.Anonymous;
    public string? UserName { get; set; }
    public string? CredentialKey { get; set; }
    public IOpcUaCredentialProvider? CredentialProvider { get; set; }

    public string? ClientCertificateThumbprint { get; set; }
    public string CertificateStorePath { get; set; } = "%LocalApplicationData%/MAS.Communication/pki";

    public bool AutoAcceptUntrustedCertificates { get; set; }

    public int SessionTimeout { get; set; } = 60000;
    public int KeepAliveInterval { get; set; } = 5000;
    public int ReconnectDelay { get; set; } = 1000;
    public bool UseEndpointDiscovery { get; set; } = true;

    public T Clone<T>() where T : ICommunicationConfig => (T)Clone();

    public object Clone() => new OpcUaConfig {
        Ip = Ip, MaxRetries = MaxRetries, ReadTimeout = ReadTimeout, WriteTimeout = WriteTimeout,
        EndpointUrl = EndpointUrl, ApplicationName = ApplicationName, ApplicationUri = ApplicationUri,
        SecurityMode = SecurityMode, SecurityPolicyUri = SecurityPolicyUri,
        IdentityType = IdentityType, UserName = UserName, CredentialKey = CredentialKey,
        CredentialProvider = CredentialProvider, ClientCertificateThumbprint = ClientCertificateThumbprint,
        CertificateStorePath = CertificateStorePath, AutoAcceptUntrustedCertificates = AutoAcceptUntrustedCertificates,
        SessionTimeout = SessionTimeout, KeepAliveInterval = KeepAliveInterval,
        ReconnectDelay = ReconnectDelay, UseEndpointDiscovery = UseEndpointDiscovery
    };
}

public sealed class MainViewModel {
    private readonly IProtocolManager _protocolManager;

    public MainViewModel(IProtocolManager protocolManager) {
        _protocolManager = protocolManager;
    }

    public async Task RunAsync() {
        // 1. 获取强类型协议实例
        IOpcUaProtocol opcua = _protocolManager.GetOrCreate<IOpcUaProtocol>(new OpcUaConfig());

        // 2. 建立连接（初始化证书、选择安全端点、校验服务端证书、建立会话、启动 KeepAlive 与重连）
        await opcua.ConnectAsync();

        // =========================
        // 示例 1：单节点读写
        // =========================

        OpcUaValue value = await opcua.ReadAsync(new OpcUaNodeId("ns=2;s=Machine.Speed"));
        if (value.IsGood) {
            Console.WriteLine($"Speed = {value.Value}，SourceTimestamp = {value.SourceTimestamp}");
        }

        double speed = await opcua.ReadAsync<double>(new OpcUaNodeId("ns=2;s=Machine.Speed"));

        await opcua.WriteAsync(new OpcUaNodeId("ns=2;s=Machine.Speed"), 1500.0);

        // =========================
        // 示例 2：批量读写（部分成功、部分失败）
        // =========================

        IReadOnlyList<OpcUaValue> readResults = await opcua.ReadAsync([
            new OpcUaReadItem(new OpcUaNodeId("ns=2;s=Machine.Speed")),
            new OpcUaReadItem(new OpcUaNodeId("ns=2;s=Machine.Temp")),
        ]);

        IReadOnlyList<OpcUaWriteResult> writeResults = await opcua.WriteAsync([
            new OpcUaWriteItem(new OpcUaNodeId("ns=2;s=Machine.Speed"), 1500.0),
            new OpcUaWriteItem(new OpcUaNodeId("ns=2;s=Machine.Mode"), 1),
        ]);

        foreach (OpcUaWriteResult result in writeResults) {
            Console.WriteLine($"{result.NodeId}: {(result.IsGood ? "OK" : "FAILED")}");
        }

        // =========================
        // 示例 3：浏览与方法调用
        // =========================

        IReadOnlyList<OpcUaBrowseNode> children = await opcua.BrowseAsync();
        foreach (OpcUaBrowseNode node in children) {
            Console.WriteLine($"{node.DisplayName} ({node.NodeClass}) -> {node.NodeId}");
        }

        OpcUaMethodResult methodResult = await opcua.CallMethodAsync(
            objectNodeId: new OpcUaNodeId("ns=2;s=Machine"),
            methodNodeId: new OpcUaNodeId("ns=2;s=Machine.Reset"),
            inputArguments: [true]);

        // =========================
        // 示例 4：数据变化订阅
        // =========================

        IOpcUaSubscription subscription = await opcua.SubscribeAsync(
            items: [
                new OpcUaMonitoredItem(new OpcUaNodeId("ns=2;s=Machine.Speed")) {
                    SamplingInterval = 500,
                    QueueSize = 1,
                    DiscardOldest = true
                }
            ],
            options: new OpcUaSubscriptionOptions {
                PublishingInterval = 500,
                KeepAliveCount = 10,
                LifetimeCount = 30,
                PublishingEnabled = true
            });

        // 事件在内部派发线程触发，不会阻塞 SDK 的 Publish 流程
        subscription.DataChanged += (_, e) => {
            Console.WriteLine($"{e.Value.NodeId}: {e.Value.Value} @ {e.Value.SourceTimestamp}");
        };

        // 动态增删监控项
        await subscription.AddItemsAsync([new OpcUaMonitoredItem(new OpcUaNodeId("ns=2;s=Machine.Temp"))]);
        await subscription.RemoveItemsAsync([new OpcUaNodeId("ns=2;s=Machine.Temp")]);

        // 释放订阅的服务端资源
        await subscription.DeleteAsync();

        // 3. 关闭连接（对象仍可再次连接）
        opcua.Disconnect();
    }
}
```

## 生命周期语义

| 方法 | OPC UA 行为 |
| ---- | ---- |
| `ConnectAsync` | 初始化应用证书、发现并选择安全端点、校验服务端证书、建立会话、启动 KeepAlive 与重连 |
| `CheckConnection` | 会话已建立、状态正常且最近 KeepAlive 未超时 |
| `ProbeConnectionAsync` | 创建临时会话完成握手/证书/身份验证后立即关闭，不影响当前正式会话 |
| `TryReconnectAsync` | 串行执行，优先恢复会话与订阅，失败后重建会话并恢复订阅 |
| `Disconnect` | 停止重连、关闭会话（订阅随会话释放），对象仍可再次连接 |
| `Dispose` | 关闭会话、订阅、证书资源与后台任务，并触发 `Disposed` 事件供管理器移除缓存 |

## 异常与返回策略

- 建立会话、认证失败：`ConnectionException`
- 单节点读取失败：`ReadErrorException`
- 单节点写入失败：`WriteErrorException`
- 取消：原样抛出 `OperationCanceledException`
- 服务级错误（浏览、方法调用、订阅、会话中断）：`OpcUaServiceException`（保留 `StatusCode` 与诊断信息）
- 证书相关错误：`OpcUaCertificateException`

> 批量读写不会因为单个节点错误（如 `BadNodeIdUnknown`）而让整个调用抛异常，而是返回逐项状态码；只有会话中断、请求无法发送等整体性错误才会抛异常。

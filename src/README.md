# MAS.Communication

## ✨ 项目特性

一个面向工业自动化场景的 **多协议通信管理框架**

- **多协议统一抽象**：Modbus TCP、MC Protocol、等协议采用一致的调用模型
- **多实例管理**：支持同协议多实例并存，适用于一机多 PLC / 多设备通信场景
- **统一生命周期控制**：统一创建、复用、查询与释放通信实例，避免资源失控
- **依赖注入集成**：基于 `Microsoft.Extensions.DependencyInjection`，便于工程化接入
- **易于扩展**：可按统一规范扩展新的通信协议，而无需改动上层业务逻辑
- **面向工业项目落地**：适用于 WPF、WinUI、后台服务等现代 .NET 应用

## 🎯 项目定位

`MAS.Communication` 是一个面向工业自动化的 .NET 通信框架，
用于在现代 .NET 应用中 **统一接入、管理和复用多种工业通信协议实例**

- 统一的通信抽象
- 多实例与多设备管理能力
- 一致的生命周期控制
- 面向依赖注入的工程化集成方式

适合构建中大型上位机系统中的通信基础设施层

## 🚀 快速开始

### 注册服务

在应用启动阶段注册通信服务：

```csharp
using Microsoft.Extensions.DependencyInjection;
using MAS.Communication;

IServiceCollection services = new ServiceCollection();
services.AddCommunication();
```

### 通信配置

在使用具体协议前，需要先提供对应的通信配置对象

框架已经为不同协议提供了统一的配置接口，调用方无需自行定义新的协议配置接口，只需要根据实际项目实现对应接口即可

例如：

- MC 协议使用：`IMcCommunicationConfig`
- Modbus 协议使用：`IModbusCommunicationConfig`

以上接口均继承自 `ICommunicationConfig`，调用方实现对应接口后，即可将配置对象传入 `IProtocolManager`，用于创建或获取协议实例

### 使用方式

通信框架通过`IProtocolManager`提供统一入口，推荐通过依赖注入方式获取并使用：

```csharp
public class MainViewModel {
    private readonly IProtocolManager _protocolManager;

    public MainViewModel(IProtocolManager protocolManager) {
        _protocolManager = protocolManager;
    }

    public async Task RunAsync() {
        // 创建配置（示例：Modbus）
        IModbusCommunicationConfig config = new ModbusCommunicationConfig {
            // 填写你的实际配置
            // 例如：Ip、Port、StationNumber 等
        };

        // 获取或创建协议实例
        IProtocol protocol = _protocolManager.GetOrCreate(config);

        // 建立连接
        await protocol.ConnectAsync();

        // 示例：执行读写（具体方法见 API 文档）
        // await protocol.ReadAsync(...);
        // await protocol.WriteAsync(...);
    }
}
```

- 所有协议实例生命周期由`IProtocolManager`统一管理
- 相同配置会自动复用已有连接实例（基于配置唯一标识）
- 支持创建多个不同配置的协议实例（例如多个 PLC）
- 当不再需要使用某个实例时，请调用`IProtocolManager.Remove(IProtocol)`或`IProtocol.Dispose()`方法释放资源
- 不建议多个线程高并发共享一个实例做高频通信，虽然目前来说它们暂时是线程安全的
- 有关更多信息，请参阅[API 文档](https://mas-copilot.github.io/mas.communication-docs/api/MAS.Communication.html)

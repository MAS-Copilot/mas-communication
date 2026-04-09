---
layout: landing
title: "MAS.Communication"
description: "面向现代 .NET 的工业通信抽象与协议管理框架"
language: "zh-CN"
---

> 面向工业自动化的统一通信框架  
> 基于依赖注入（DI）使用，提供协议实例管理、连接复用与统一读写入口

---

## ⚠️ 使用前提

在使用本项目之前，请确认你的应用满足以下条件：

- 目标框架支持 `net10.0`、`net9.0`、`net8.0` 或 `net481`
- 使用 `Microsoft.Extensions.DependencyInjection` 进行服务注册与解析
- 适合在 WPF、WinForms、WinUI、Worker、ASP.NET Core 等应用中集成

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

在使用具体协议前，需要先提供对应的通信配置对象。框架为不同协议提供了统一配置接口：

| 协议 | 配置接口 |
| ---- | ---- |
| MC（Mitsubishi） | `IMcCommunicationConfig` |
| ModbusTCP | `IModbusCommunicationConfig` |
| S7（Siemens） | `IS7CommunicationConfig` |

以上接口均继承自 `ICommunicationConfig`，调用方需继承并实现它

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
- 不建议多个线程高并发共享一个实例做高频通信，虽然目前来说它们都是暂时安全的

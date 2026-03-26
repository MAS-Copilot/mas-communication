# MAS.Communication.S7Protocol

以下示例演示如何通过 `IProtocolManager` 获取 `IS7Protocol` 实例，并完成 S7 协议的常用读写操作

```csharp
using MAS.Communication;
using MAS.Communication.S7Protocol;
using S7.Net;
using S7.Net.Types;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ProductData {
    public short ProductId;
    public short Result;
    public int Count;
}

public sealed class MainViewModel {
    private readonly IProtocolManager _protocolManager;

    public MainViewModel(IProtocolManager protocolManager) {
        _protocolManager = protocolManager;
    }

    public async Task RunAsync() {
        // 1. 创建 S7 通信配置
        IS7CommunicationConfig config = new S7CommunicationConfig {
            Ip = "192.168.0.10",
            Type = "S1200",
            Rack = 0,
            Slot = 1
        };

        // 2. 获取强类型协议实例
        IS7Protocol protocol = _protocolManager.GetOrCreate<IS7Protocol>(config);

        // 3. 建立连接
        await protocol.ConnectAsync();

        // =========================
        // 示例 1：普通变量读写
        // =========================

        // 从 DB1.DBW0 读取 1 个 Word
        object? readValue = await protocol.ReadAsync(
            dataType: DataType.DataBlock,
            db: 1,
            startByteAdr: 0,
            varType: VarType.Word,
            varCount: 1);

        // 向 DB1.DBW0 写入一个 Word 值
        await protocol.WriteAsync(
            dataType: DataType.DataBlock,
            db: 1,
            startByteAdr: 0,
            value: (ushort)123);

        // =========================
        // 示例 2：结构体读写
        // =========================

        // 从 DB10 起始地址 0 读取结构体
        ProductData? product = await protocol.ReadStructAsync<ProductData>(
            db: 10,
            startByteAdr: 0);

        // 向 DB10 起始地址 0 写入结构体
        await protocol.WriteStructAsync(
            structValue: new ProductData {
                ProductId = 1001,
                Result = 1,
                Count = 25
            },
            db: 10,
            startByteAdr: 0);
    }
}
```

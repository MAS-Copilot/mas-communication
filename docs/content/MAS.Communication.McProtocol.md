# MAS.Communication.McProtocol

以下示例演示如何通过 `IProtocolManager` 获取 `IMcProtocol` 实例，并完成 MC 协议的常用读写操作

```csharp
using MAS.Communication;
using MAS.Communication.McProtocol;

public sealed class MainViewModel {
    private readonly IProtocolManager _protocolManager;

    public MainViewModel(IProtocolManager protocolManager) {
        _protocolManager = protocolManager;
    }

    public async Task RunAsync() {
        // 1. 创建 MC 通信配置
        IMcCommunicationConfig config = new McCommunicationConfig {
            Ip = "192.168.0.10",
            Port = 6000,
            ProtocolFrame = McFrame.MC3E
        };

        // 2. 获取强类型协议实例
        IMcProtocol protocol = _protocolManager.GetOrCreate<IMcProtocol>(config);

        // 3. 建立连接
        await protocol.ConnectAsync();

        // =========================
        // 示例 1：读写字数据
        // =========================

        // 从 D100 开始读取 4 个字
        short[] words = await protocol.ReadWordsAsync(
            deviceType: "D",
            startAddress: 100,
            length: 4);

        // 向 D100 开始写入 4 个字
        await protocol.WriteWordsAsync(
            deviceType: "D",
            startAddress: 100,
            values: new short[] { 10, 20, 30, 40 });

        // =========================
        // 示例 2：读写位数据
        // =========================

        // 从 M100 开始读取 8 个位
        bool[] bits = await protocol.ReadBitsAsync(
            deviceType: "M",
            startAddress: 100,
            length: 8);

        // 向 M100 开始写入 8 个位
        await protocol.WriteBitsAsync(
            deviceType: "M",
            startAddress: 100,
            values: new bool[] { true, false, true, false, true, false, false, true });
    }
}
```

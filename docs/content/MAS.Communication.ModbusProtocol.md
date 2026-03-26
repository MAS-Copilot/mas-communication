# MAS.Communication.ModbusProtocol

以下示例演示如何通过 `IProtocolManager` 使用 Modbus TCP 协议完成连接、读取与写入操作

```csharp
using MAS.Communication;
using MAS.Communication.ModbusProtocol;

public sealed class MainViewModel {
    private readonly IProtocolManager _protocolManager;

    public MainViewModel(IProtocolManager protocolManager) {
        _protocolManager = protocolManager;
    }

    public async Task RunAsync() {
        // 1. 创建 Modbus 通信配置
        IModbusCommunicationConfig config = new ModbusCommunicationConfig {
            Ip = "192.168.0.10",
            Port = 502,
            UnitId = 1,

            // 地址是否按 1 开始
            UseOneBasedAddress = false,

            // 根据设备实际字节序配置
            ByteOrder = EndianType.BigEndian,
            WordOrder = WordOrderType.HighLow,

            // 超时时间（毫秒）
            ReadTimeout = 3000,
            WriteTimeout = 3000
        };

        // 2. 获取强类型协议实例
        IModbusProtocol protocol = _protocolManager.GetOrCreate<IModbusProtocol>(config);

        // 3. 建立连接
        await protocol.ConnectAsync();

        // =========================
        // 示例 1：读写保持寄存器
        // =========================

        // 从保持寄存器地址 0 开始读取 4 个寄存器
        ushort[] registers = await protocol.ReadRegistersAsync(
            ModbusDataArea.HoldingRegisters,
            startAddress: 0,
            count: 4);

        // 向保持寄存器地址 0 开始写入 4 个寄存器
        await protocol.WriteRegistersAsync(
            startAddress: 0,
            values: new ushort[] { 100, 200, 300, 400 });

        // =========================
        // 示例 2：读写线圈
        // =========================

        // 从线圈地址 0 开始读取 8 个布尔量
        bool[] coils = await protocol.ReadBitsAsync(
            ModbusDataArea.Coils,
            startAddress: 0,
            count: 8);

        // 向线圈地址 0 开始写入布尔量
        await protocol.WriteBitsAsync(
            startAddress: 0,
            values: new bool[] { true, false, true, false });
    }
}
```

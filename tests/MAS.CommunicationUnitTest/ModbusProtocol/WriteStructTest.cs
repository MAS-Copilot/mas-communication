// =============================================================================
// Professional Automation Equipment Manufacturer.
//
// Documentation: https://mas-copilot.github.io/MAS.DataMaster-Docs/
// Website:       https://www.mas-automation.com/
//
// Copyright (c) MAS(厦门威光) Corporation. All rights reserved.
// =============================================================================

using MAS.Communication;
using MAS.Communication.ModbusProtocol;
using Moq;

namespace MAS.CommunicationUnitTest.ModbusProtocol;

[TestClass]
[DoNotParallelize]
public sealed class WriteStructTest : IDisposable {
    private readonly IModbusCommunicationConfig _config;
    private readonly ModbusProtocolTcp _protocol;
    private bool _disposedValue;
    private readonly ushort _writeDataAdr = 1000;

    public TestContext TestContext { get; set; }

    public WriteStructTest() {       
        var mockConfig = new Mock<IModbusCommunicationConfig>();

        _ = mockConfig.Setup(c => c.Ip).Returns("127.0.0.1");
        _ = mockConfig.Setup(c => c.Port).Returns(502);

        _ = mockConfig.Setup(c => c.UnitId).Returns(1);
        _ = mockConfig.Setup(c => c.UseOneBasedAddress).Returns(false);

        _ = mockConfig.Setup(c => c.ByteOrder).Returns(ModbusByteOrder.BigEndian);
        _ = mockConfig.Setup(c => c.WordOrder).Returns(ModbusWordOrder.Normal);

        _config = mockConfig.Object;
        _protocol = new ModbusProtocolTcp(_config);
    }

    [TestInitialize]
    public async Task Initialize() {
        TestContext.WriteLine("初始化测试环境...");

        if (!_protocol.CheckConnection()) {
            try {
                TestContext.WriteLine($"尝试连接到设备. IP：{_config.Ip} Port：{_config.Port} UnitId：{_config.UnitId} OneBased：{_config.UseOneBasedAddress}");
                await _protocol.ConnectAsync(TestContext.CancellationTokenSource.Token).ConfigureAwait(false);
            } catch (ConnectionException ex) {
                Assert.Inconclusive($"{ex.Message}, 测试结束");
            }

            if (!_protocol.CheckConnection()) {
                Assert.Inconclusive("未能连接到设备，测试结束");
            }
        }
    }

    [TestMethod]
    public async Task 结构体写入后寄存器内容校验_HoldingRegisters() {
        TestContext.WriteLine($"Title--验证 Struct 写入后寄存器内容是否符合预期（HoldingRegisters），目标地址：{_writeDataAdr}");
        TestContext.WriteLine($"ByteOrder={_config.ByteOrder}, WordOrder={_config.WordOrder}");

        var payload = ModbusTestPayload.CreateRandom();

        TestContext.WriteLine("NO.1--计算期望寄存器数组（基于 StructBinaryHelper + ModbusRegisterBinaryHelper）");
        byte[] expectedBytes = StructBinaryHelper.StructToBytes(payload);
        ushort[] expectedRegisters = ModbusRegisterBinaryHelper.LittleEndianBytesToRegisters(
            expectedBytes,
            _config.ByteOrder,
            _config.WordOrder
        );

        TestContext.WriteLine($"结构体字节长度={expectedBytes.Length}，期望寄存器数量={expectedRegisters.Length}");

        TestContext.WriteLine("NO.2--写入结构体");
        await _protocol.WriteStructAsync(            
            startAddress: _writeDataAdr,
            value: payload,
            cts: TestContext.CancellationTokenSource.Token
        ).ConfigureAwait(false);

        TestContext.WriteLine("NO.3--直接读取 HoldingRegisters 验证寄存器内容");
        ushort[] actualRegisters = await _protocol.ReadRegistersAsync(
            area: ModbusDataArea.HoldingRegisters,
            startAddress: _writeDataAdr,
            count: (ushort)expectedRegisters.Length,
            cts: TestContext.CancellationTokenSource.Token
        ).ConfigureAwait(false);

        TestContext.WriteLine("断言：寄存器数组是否相等");
        try {
            CollectionAssert.AreEqual(expectedRegisters, actualRegisters, "写入后寄存器内容与期望不匹配！");
        } catch {
            TestContext.WriteLine("寄存器数组不匹配，以下为不匹配的元素：");
            int mismatch = 0;
            int length = Math.Min(expectedRegisters.Length, actualRegisters.Length);

            for (int i = 0; i < length; i++) {
                if (expectedRegisters[i] != actualRegisters[i]) {
                    mismatch++;
                    TestContext.WriteLine($"索引 {i}： 期望 0x{expectedRegisters[i]:X4}，实际 0x{actualRegisters[i]:X4}");
                    if (mismatch >= 50) {
                        TestContext.WriteLine("不匹配过多，仅输出前 50 个差异。");
                        break;
                    }
                }
            }

            TestContext.WriteLine($"ExpectedLength={expectedRegisters.Length}, ActualLength={actualRegisters.Length}");
            throw;
        }
    }

    [TestMethod]
    [DataRow(ModbusByteOrder.BigEndian, ModbusWordOrder.Normal)]
    [DataRow(ModbusByteOrder.BigEndian, ModbusWordOrder.Swap)]
    [DataRow(ModbusByteOrder.LittleEndian, ModbusWordOrder.Normal)]
    [DataRow(ModbusByteOrder.LittleEndian, ModbusWordOrder.Swap)]
    public void 字节序字序往返一致性测试_纯内存(ModbusByteOrder byteOrder, ModbusWordOrder wordOrder) {
        TestContext.WriteLine($"Title--纯内存往返测试：ByteOrder={byteOrder}, WordOrder={wordOrder}");

        var payload = ModbusTestPayload.CreateDeterministic();

        byte[] bytes = StructBinaryHelper.StructToBytes(payload);
        ushort[] regs = ModbusRegisterBinaryHelper.LittleEndianBytesToRegisters(bytes, byteOrder, wordOrder);
        byte[] bytes2 = ModbusRegisterBinaryHelper.RegistersToLittleEndianBytes(regs, bytes.Length, byteOrder, wordOrder);

        CollectionAssert.AreEqual(bytes, bytes2, "bytes -> registers -> bytes 往返后不一致！");
    }

    public void Dispose() {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing) {
        if (!_disposedValue) {
            if (disposing) {
                _protocol.Dispose();
            }

            _disposedValue = true;
        }
    }
}

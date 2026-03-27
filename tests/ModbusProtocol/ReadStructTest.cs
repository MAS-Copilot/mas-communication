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
using MAS.Communication.ModbusProtocol;
using Moq;

namespace MAS.CommunicationUnitTest.ModbusProtocol;

[TestClass]
[DoNotParallelize]
public sealed class ReadStructTest : IDisposable {
    private readonly Mock<IModbusCommunicationConfig> _mockConfig;
    private readonly IModbusCommunicationConfig _config;
    private readonly ModbusProtocolTcp _protocol;
    private bool _disposedValue;
    private readonly ushort _writeDataAdr = 1000;

    public TestContext TestContext { get; set; }

    public ReadStructTest() {       
        _mockConfig = new Mock<IModbusCommunicationConfig>();

        _ = _mockConfig.Setup(c => c.Ip).Returns("127.0.0.1");
        _ = _mockConfig.Setup(c => c.Port).Returns(502);

        _ = _mockConfig.Setup(c => c.UnitId).Returns(1);
        _ = _mockConfig.Setup(c => c.UseOneBasedAddress).Returns(false);

        _ = _mockConfig.Setup(c => c.ByteOrder).Returns(ModbusByteOrder.BigEndian);
        _ = _mockConfig.Setup(c => c.WordOrder).Returns(ModbusWordOrder.Normal);

        _config = _mockConfig.Object;
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
    public async Task 结构体写入读取测试_HoldingRegisters() {
        TestContext.WriteLine($"Title--验证 Struct 写入读取测试（HoldingRegisters），目标地址：{_writeDataAdr}");
        TestContext.WriteLine($"ByteOrder={_config.ByteOrder}, WordOrder={_config.WordOrder}");

        var payload = ModbusTestPayload.CreateRandom();

        TestContext.WriteLine("NO.1--写入结构体");
        await _protocol.WriteStructAsync(
            startAddress: _writeDataAdr,
            value: payload,
            cts: TestContext.CancellationTokenSource.Token
        ).ConfigureAwait(false);

        TestContext.WriteLine("NO.2--读取结构体");
        var resultObj = await _protocol.ReadStructAsync<ModbusTestPayload>(
            area: ModbusDataArea.HoldingRegisters,
            startAddress: _writeDataAdr,
            cts: TestContext.CancellationTokenSource.Token
        ).ConfigureAwait(false);

        var result = resultObj;

        TestContext.WriteLine("断言：结构体字节序列是否相等（按 StructBinaryHelper 编解码结果对比）");
        byte[] expectedBytes = StructBinaryHelper.StructToBytes(payload);
        byte[] actualBytes = StructBinaryHelper.StructToBytes(result);

        try {
            CollectionAssert.AreEqual(expectedBytes, actualBytes, "写入结构体与读取结构体的字节序列不匹配！");
        } catch {
            TestContext.WriteLine("结构体字节序列不匹配，输出前 64 个字节差异：");
            int max = Math.Min(Math.Min(expectedBytes.Length, actualBytes.Length), 64);
            for (int i = 0; i < max; i++) {
                if (expectedBytes[i] != actualBytes[i]) {
                    TestContext.WriteLine($"Offset {i}: Write=0x{expectedBytes[i]:X2}, Read=0x{actualBytes[i]:X2}");
                }
            }

            TestContext.WriteLine($"ExpectedBytesLength={expectedBytes.Length}, ActualBytesLength={actualBytes.Length}");
            throw;
        }
    }

    [TestMethod]
    [DataRow(ModbusByteOrder.BigEndian, ModbusWordOrder.Normal)]
    [DataRow(ModbusByteOrder.BigEndian, ModbusWordOrder.Swap)]
    [DataRow(ModbusByteOrder.LittleEndian, ModbusWordOrder.Normal)]
    [DataRow(ModbusByteOrder.LittleEndian, ModbusWordOrder.Swap)]
    public void 结构体编解码往返测试_纯内存(ModbusByteOrder byteOrder, ModbusWordOrder wordOrder) {
        TestContext.WriteLine($"Title--纯内存 Struct 往返测试：ByteOrder={byteOrder}, WordOrder={wordOrder}");

        var payload = ModbusTestPayload.CreateDeterministic();

        byte[] bytes = StructBinaryHelper.StructToBytes(payload);
        ushort[] regs = ModbusRegisterBinaryHelper.LittleEndianBytesToRegisters(bytes, byteOrder, wordOrder);

        byte[] bytes2 = ModbusRegisterBinaryHelper.RegistersToLittleEndianBytes(regs, bytes.Length, byteOrder, wordOrder);
        object obj2 = StructBinaryHelper.BytesToStruct(bytes2, typeof(ModbusTestPayload));

        Assert.IsNotNull(obj2, "往返解码结果为 null");

        byte[] expectedBytes = bytes;
        byte[] actualBytes = StructBinaryHelper.StructToBytes(obj2);

        CollectionAssert.AreEqual(expectedBytes, actualBytes, "纯内存往返后的结构体字节序列不一致！");
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

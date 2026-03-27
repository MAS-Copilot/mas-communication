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
public sealed class ReadRegistersTest : IDisposable {
    private readonly IModbusCommunicationConfig _config;
    private readonly ModbusProtocolTcp _protocol;
    private bool _disposedValue;
    private ushort[] _registers = [];
    private readonly ushort _writeDataAdr = 1000;

    public TestContext TestContext { get; set; }

    public ReadRegistersTest() {       
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

        InitializeTestData();
    }

    [TestMethod]
    public async Task 保持寄存器写入读取测试_HoldingRegisters() {
        int length = _registers.Length;

        TestContext.WriteLine($"Title--验证 HoldingRegisters ushort[] 写入读取测试，目标地址：{_writeDataAdr}，数组长度：{length}");

        TestContext.WriteLine("NO.1--写入生成的随机 ushort[]");
        await _protocol.WriteRegistersAsync(
            startAddress: _writeDataAdr,
            values: _registers,
            cts: TestContext.CancellationTokenSource.Token
        ).ConfigureAwait(false);

        TestContext.WriteLine("NO.2--读取 HoldingRegisters ushort[]");
        ushort[] result = await _protocol.ReadRegistersAsync(
            area: ModbusDataArea.HoldingRegisters,
            startAddress: _writeDataAdr,
            count: (ushort)length,
            cts: TestContext.CancellationTokenSource.Token
        ).ConfigureAwait(false);

        TestContext.WriteLine("NO.3--断言：ushort[] 数据是否相等");
        try {
            CollectionAssert.AreEqual(_registers, result, "写入的数据与读取的数据不匹配！");
        } catch {
            TestContext.WriteLine("数组不匹配，以下为不匹配的元素：");
            int mismatch = 0;

            for (int i = 0; i < length; i++) {
                if (_registers[i] != result[i]) {
                    mismatch++;
                    TestContext.WriteLine($"索引 {i}： 写入值 {_registers[i]}，读取值 {result[i]}");
                    if (mismatch >= 50) {
                        TestContext.WriteLine("不匹配过多，仅输出前 50 个差异。");
                        break;
                    }
                }
            }

            throw;
        }
    }

    [TestMethod]
    public async Task 输入寄存器读取测试_InputRegisters() {
        int length = Math.Min(_registers.Length, 125);

        TestContext.WriteLine($"Title--验证 InputRegisters ushort[] 读取测试，目标地址：{_writeDataAdr}，数组长度：{length}");

        TestContext.WriteLine("NO.1--读取 InputRegisters ushort[]");
        ushort[] result = await _protocol.ReadRegistersAsync(
            area: ModbusDataArea.InputRegisters,
            startAddress: _writeDataAdr,
            count: (ushort)length,
            cts: TestContext.CancellationTokenSource.Token
        ).ConfigureAwait(false);

        TestContext.WriteLine($"NO.2--断言：长度是否一致：{result.Length}");
        Assert.AreEqual(length, result.Length, "读取长度与期望不一致！");
    }

    public void Dispose() {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    private void InitializeTestData() {
        TestContext.WriteLine("随机生成 ushort[] 寄存器数组进行测试");
        Random rand = new();
        int length = rand.Next(1, 123);

        _registers = new ushort[length];
        for (int i = 0; i < _registers.Length; i++) {
            _registers[i] = (ushort)rand.Next(0, ushort.MaxValue + 1);
        }

        TestContext.WriteLine($"随机生成的寄存器数组长度为: {_registers.Length}\n");
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

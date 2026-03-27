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
public sealed class StressWriteReadStructTest : IDisposable {
    private const int Iterations = 100;

    private readonly IModbusCommunicationConfig _config;
    private readonly ModbusProtocolTcp _protocol;
    private bool _disposedValue;
    private readonly ushort _writeDataAdr = 1000;
    private readonly Random _rand = Random.Shared;

    public TestContext TestContext { get; set; }

    public StressWriteReadStructTest() {
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

        if (_protocol.CheckConnection()) {
            return;
        }

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

    [TestMethod]
    public async Task 结构体写入读取压力测试_字节对比_HoldingRegisters() {
        TestContext.WriteLine($"Title--Struct 写入读取压力测试（HoldingRegisters），目标地址：{_writeDataAdr}，次数：{Iterations}");
        TestContext.WriteLine($"ByteOrder = {_config.ByteOrder}, WordOrder = {_config.WordOrder}");

        ModbusStressPayload? lastPayload = null;

        for (int i = 1; i <= Iterations; i++) {
            TestContext.CancellationTokenSource.Token.ThrowIfCancellationRequested();

            var payload = ModbusStressPayload.CreateRandom(_rand, lastPayload);
            await _protocol.WriteStructAsync(                
                startAddress: _writeDataAdr,
                value: payload,
                cts: TestContext.CancellationTokenSource.Token
            ).ConfigureAwait(false);

            var resultObj = await _protocol.ReadStructAsync<ModbusStressPayload>(
                area: ModbusDataArea.HoldingRegisters,
                startAddress: _writeDataAdr,
                cts: TestContext.CancellationTokenSource.Token
            ).ConfigureAwait(false);

            var result = resultObj;

            byte[] expectedBytes = StructBinaryHelper.StructToBytes(payload);
            byte[] actualBytes = StructBinaryHelper.StructToBytes(result);

            try {
                CollectionAssert.AreEqual(expectedBytes, actualBytes, $"第 {i} 次写入/读取字节序列不一致！");
            } catch {
                TestContext.WriteLine("不匹配，输出前 96 个字节差异：");
                int max = Math.Min(Math.Min(expectedBytes.Length, actualBytes.Length), 96);
                for (int b = 0; b < max; b++) {
                    if (expectedBytes[b] != actualBytes[b]) {
                        TestContext.WriteLine($"Offset {b}: Write=0x{expectedBytes[b]:X2}, Read=0x{actualBytes[b]:X2}");
                    }
                }

                TestContext.WriteLine($"ExpectedBytesLength={expectedBytes.Length}, ActualBytesLength={actualBytes.Length}");
                throw;
            }

            lastPayload = payload;
        }
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

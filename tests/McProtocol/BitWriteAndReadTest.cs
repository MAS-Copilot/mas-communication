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
using MAS.Communication.McProtocol;
using Moq;

namespace MAS.CommunicationUnitTest.McProtocol;

[TestClass]
public class BitWriteAndReadTest : IDisposable {
    private readonly IMcCommunicationConfig _config;
    private readonly McProtocolClient _protocol;
    private bool _disposedValue;
    private bool[] _bools = [];
    private readonly ushort _writeDataAdr = 1000;

    public TestContext TestContext { get; set; }

    public BitWriteAndReadTest() {       
        var mockConfig = new Mock<IMcCommunicationConfig>();
        _ = mockConfig.Setup(c => c.Ip).Returns("192.168.10.1");
        _ = mockConfig.Setup(c => c.ProtocolFrame).Returns(McFrame.MC3E);
        _ = mockConfig.Setup(c => c.Port).Returns(3000);

        _config = mockConfig.Object;
        _protocol = new McProtocolClient(_config);
    }

    [TestInitialize]
    public async Task Initialize() {
        TestContext.WriteLine("初始化测试环境...");

        try {
            TestContext.WriteLine($"尝试连接到PLC. 端口：{_config.Port} IP：{_config.Ip} 协议帧：{_config.ProtocolFrame}");
            await _protocol.ConnectAsync(TestContext.CancellationTokenSource.Token).ConfigureAwait(false);
        } catch (ConnectionException ex) {
            Assert.Inconclusive($"{ex.Message}, 测试结束");
        }

        if (!_protocol.CheckConnection()) {
            Assert.Inconclusive("未能连接到PLC，测试结束");
        }

        TestContext.WriteLine("随机生成 bit 数组进行测试");
        Random rand = new();
        int length = rand.Next(1, 999);

        _bools = new bool[length];
        for (int i = 0; i < _bools.Length; i++) {
            _bools[i] = rand.Next(2) == 0;
        }

        TestContext.WriteLine($"随机生成的 bit 数组长度为: {_bools.Length}\n");
    }

    [TestMethod]
    public async Task 位数据写入读取测试1() {
        var address = _writeDataAdr;
        var length = _bools.Length;
        var register = "D";
        TestContext.WriteLine($"Tile--验证位 bit[] 写入测试， 目标寄存器：{register}，目标地址：{address}，数组长度：{length}");

        TestContext.WriteLine("NO.1--写入生成的随机 bit[]");
        await _protocol.WriteBitsAsync(
            register,
            address,
            _bools,
            TestContext.CancellationTokenSource.Token).ConfigureAwait(false);

        TestContext.WriteLine("NO.2--读取寄存器的 bit[]");
        var result = await _protocol.ReadBitsAsync(
            register,
            address,
            length,
            TestContext.CancellationTokenSource.Token).ConfigureAwait(false);

        TestContext.WriteLine("断言：bit[] 数据是否相等");

        try {
            CollectionAssert.AreEqual(_bools, result, "写入的数据与读取的数据不匹配！");
        } catch {
            TestContext.WriteLine("数组不匹配，以下为不匹配的元素：");
            for (int i = 0; i < length; i++) {
                if (_bools[i] != result[i]) {
                    TestContext.WriteLine($"索引 {i}： 写入值 {_bools[i]}，读取值 {result[i]}");
                }
            }

            throw;
        }
    }

    [TestMethod]
    public async Task 位数据写入读取测试2() {
        var address = _writeDataAdr;
        var length = _bools.Length;
        var register = "M";
        TestContext.WriteLine($"Tile--验证位 bit[] 写入测试， 目标寄存器：{register}，目标地址：{address}，数组长度：{length}");

        TestContext.WriteLine("NO.1--写入生成的随机 bit[]");
        await _protocol.WriteBitsAsync(
            register,
            address,
            _bools,
            TestContext.CancellationTokenSource.Token).ConfigureAwait(false);

        TestContext.WriteLine("NO.2--读取寄存器的 bit[]");
        var result = await _protocol.ReadBitsAsync(
            register,
            address,
            length,
            TestContext.CancellationTokenSource.Token).ConfigureAwait(false);

        TestContext.WriteLine("断言：bit[] 数据是否相等");

        try {
            CollectionAssert.AreEqual(_bools, result, "写入的数据与读取的数据不匹配！");
        } catch {
            TestContext.WriteLine("数组不匹配，以下为不匹配的元素：");
            for (int i = 0; i < length; i++) {
                if (_bools[i] != result[i]) {
                    TestContext.WriteLine($"索引 {i}： 写入值 {_bools[i]}，读取值 {result[i]}");
                }
            }

            throw;
        }
    }

    protected virtual void Dispose(bool disposing) {
        if (!_disposedValue) {
            if (disposing) {
                _protocol.Dispose();
            }

            _disposedValue = true;
        }
    }

    public void Dispose() {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }    
}

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
public class StructWriteAndReadTest : IDisposable {
    private readonly IMcCommunicationConfig _config;
    private readonly McProtocolClient _protocol;
    private bool _disposedValue;
    private readonly ushort _writeDataAdr = 1000;

    public StructWriteAndReadTest() {       
        var mockConfig = new Mock<IMcCommunicationConfig>();
        _ = mockConfig.Setup(c => c.Ip).Returns("192.168.10.1");
        _ = mockConfig.Setup(c => c.ProtocolFrame).Returns(McFrame.MC3E);
        _ = mockConfig.Setup(c => c.Port).Returns(3000);

        _config = mockConfig.Object;
        _protocol = new McProtocolClient(_config);
    }

    [TestInitialize]
    public async Task Initialize() {
        TestContext.WriteLine("初始化结构体测试环境...");

        try {
            TestContext.WriteLine($"尝试连接到PLC. 端口：{_config.Port} IP：{_config.Ip} 协议帧：{_config.ProtocolFrame}");
            await _protocol.ConnectAsync(TestContext.CancellationTokenSource.Token).ConfigureAwait(false);
        } catch (ConnectionException ex) {
            Assert.Inconclusive($"{ex.Message}, 测试结束");
        }

        if (!_protocol.CheckConnection()) {
            Assert.Inconclusive("未能连接到PLC，结构体测试结束");
        }

        TestContext.WriteLine("PLC 连接成功，开始结构体读写测试...\n");
    }

    [TestMethod]
    public async Task 结构体数据写入读取测试1() {
        var address = _writeDataAdr;
        var register = "D";
        TestContext.WriteLine($"Tile--验证 EquipmentDataStruct 结构体写入读取测试， 目标寄存器：{register}，目标地址：{address}");

        EquipmentDataStruct writeData = new() {
            IsCreate = true,
            IsRead = false,
            IsUpdate = true,
            IsDelete = false,
            IsAddOrUpdate = true,
            IsNewFile = false,
            EquipmentName = "EquipmentName",
            EquipmentType = "EquipmentType",
            SerialNumber = "SN-1234567890",
            Status = "Status",
            Manufacturer = "Manufacturer",
            Model = "EQ-Model-ABC",
            Location = "Location",
            Notes = "Notes"
        };

        TestContext.WriteLine("NO.1--写入 EquipmentDataStruct 结构体数据");
        await _protocol.WriteStructAsync(
            "D",            
            address,
            writeData, 
            TestContext.CancellationTokenSource.Token).ConfigureAwait(false);

        TestContext.WriteLine("NO.2--读取 PLC 寄存器的 EquipmentDataStruct 结构体数据");
        var readResult = await _protocol.ReadStructAsync<EquipmentDataStruct>(
            "D",
           address,
           TestContext.CancellationTokenSource.Token).ConfigureAwait(false);

        TestContext.WriteLine("NO.3--断言：EquipmentDataStruct 结构体数据是否相等");
        AssertEquipmentDataStructAreEqual(writeData, (EquipmentDataStruct)readResult);
    }

    [TestMethod]
    public async Task 结构体数据写入读取测试2() {
        var address = _writeDataAdr + 150;
        var register = "D";
        TestContext.WriteLine($"Tile--验证 ProductInfoStruct 结构体写入读取测试， 目标寄存器：{register}，目标地址：{address}");

        ProductInfoStruct writeData = new() {
            IsCreate = false,
            IsRead = true,
            IsUpdate = false,
            IsDelete = true,
            IsAddOrUpdate = false,
            IsNewFile = true,
            EquipmentId = 100,
            ProductId = "PRODUCT-ID-002",
            ProductCode = "PC-CODE-456",
            RecipeId = "RECIPE-789",
            ProductName = "ProductName-002",
            Category = "Category-B",
            Notes = "Notes"
        };

        TestContext.WriteLine("NO.1--写入 ProductInfoStruct 结构体数据");
        await _protocol.WriteStructAsync("D", address, writeData, TestContext.CancellationTokenSource.Token).ConfigureAwait(false);

        TestContext.WriteLine("NO.2--使用非泛型方法读取 PLC 寄存器的 ProductInfoStruct 结构体数据");
        var nonGenericResult = await _protocol.ReadStructAsync<ProductInfoStruct>("D", address, TestContext.CancellationTokenSource.Token).ConfigureAwait(false);

        TestContext.WriteLine("NO.3--使用泛型方法读取 PLC 寄存器的 ProductInfoStruct 结构体数据");
        var genericReadResult = await _protocol.ReadStructAsync<ProductInfoStruct>("D", address, TestContext.CancellationTokenSource.Token).ConfigureAwait(false);

        TestContext.WriteLine("NO.4--断言：泛型方法与非泛型方法读取结果是否相等");
        AssertProductInfoStructAreEqual(genericReadResult, nonGenericResult!);

        TestContext.WriteLine("NO.5--断言：写入数据与泛型方法读取数据是否相等");
        AssertProductInfoStructAreEqual(writeData, genericReadResult);
    }

    [TestMethod]
    public async Task 结构体数据写入读取测试3() {
        var address = _writeDataAdr + 300;
        var register = "D";
        TestContext.WriteLine($"Tile--验证 MixedDataStruct 结构体写入读取测试， 目标寄存器：{register}，目标地址：{address}");

        MixedDataStruct writeData = new() {
            IsCreate = true,
            IsRead = true,
            IsUpdate = false,
            IsDelete = false,
            IsAddOrUpdate = true,
            IsNewFile = true,
            IsActive = true,
            IsAlarm = false,
            IsOperational = true,
            IsError = false,
            Id = 12345,
            Temperature = 25.5f,
            Pressure = 101.325,
            Volume = 500,
            DeviceName = "DeviceName-003",
            ManufacturerName = "ManufacturerName-XYZ"
        };

        TestContext.WriteLine("NO.1--写入 MixedDataStruct 结构体数据");
        await _protocol.WriteStructAsync(
            "D",            
            address,
            writeData,
            TestContext.CancellationTokenSource.Token).ConfigureAwait(false);

        TestContext.WriteLine("NO.2--读取 PLC 寄存器的 MixedDataStruct 结构体数据");
        var readResult = await _protocol.ReadStructAsync<MixedDataStruct>(
            "D",
            address,
            TestContext.CancellationTokenSource.Token).ConfigureAwait(false);

        TestContext.WriteLine("NO.3--断言：MixedDataStruct 结构体数据是否相等");
        AssertMixedDataStructAreEqual(writeData, readResult);
    }

    [TestMethod]
    public async Task 结构体数据写入读取测试4() {
        var address = _writeDataAdr;
        var register = "D";
        TestContext.WriteLine($"Tile--验证 TemporaryTest 结构体写入读取测试， 目标寄存器：{register}，目标地址：{address}");

        TemporaryTest writeData = TestDataHelper.GenerateRandomTestData<TemporaryTest>();

        TestContext.WriteLine("NO.1--写入 TemporaryTest 结构体数据");

        await _protocol.WriteStructAsync("D", address, writeData, TestContext.CancellationTokenSource.Token).ConfigureAwait(false);

        TestContext.WriteLine($"NO.2--映射表：\n{TestDataHelper.GetStructValues(writeData, address)}\n");

        TestContext.WriteLine("NO.3--读取 PLC 寄存器的 TemporaryTest 结构体数据");
        var readResult = await _protocol.ReadStructAsync<TemporaryTest>(
            "D",
            address,
            TestContext.CancellationTokenSource.Token).ConfigureAwait(false);

        byte[] writeDataBytes = StructBinaryHelper.StructToBytes(writeData);
        byte[] readDataBytes = StructBinaryHelper.StructToBytes(readResult!);

        TestContext.WriteLine($"NO.4--断言：验证字节长度是否一致，预期长度：{writeDataBytes.Length}");
        Assert.AreEqual(writeDataBytes.Length, readDataBytes.Length, "字节数组长度不一致");

        TestContext.WriteLine("NO.5--断言：验证字节数组的值是否一致");
        for (int i = 0; i < writeDataBytes.Length; i++) {
            Assert.AreEqual(writeDataBytes[i], readDataBytes[i], $"字节数组在位置 {i} 的值不一致");
        }

        TestContext.WriteLine("NO.6--断言：TemporaryTest 结构体数据是否相等");
        AssertTemporaryTestAreEqual(writeData, readResult!);
    }

    #region 结构体数据比较方法

    private static void AssertEquipmentDataStructAreEqual(EquipmentDataStruct expected, EquipmentDataStruct actual) {
        Assert.AreEqual(expected.IsCreate, actual.IsCreate, "EquipmentDataStruct.IsCreate 不匹配");
        Assert.AreEqual(expected.IsRead, actual.IsRead, "EquipmentDataStruct.IsRead 不匹配");
        Assert.AreEqual(expected.IsUpdate, actual.IsUpdate, "EquipmentDataStruct.IsUpdate 不匹配");
        Assert.AreEqual(expected.IsDelete, actual.IsDelete, "EquipmentDataStruct.IsDelete 不匹配");
        Assert.AreEqual(expected.IsAddOrUpdate, actual.IsAddOrUpdate, "EquipmentDataStruct.IsAddOrUpdate 不匹配");
        Assert.AreEqual(expected.IsNewFile, actual.IsNewFile, "EquipmentDataStruct.IsNewFile 不匹配");
        Assert.AreEqual(expected.EquipmentName, actual.EquipmentName, "EquipmentDataStruct.EquipmentName 不匹配");
        Assert.AreEqual(expected.EquipmentType, actual.EquipmentType, "EquipmentDataStruct.EquipmentType 不匹配");
        Assert.AreEqual(expected.SerialNumber, actual.SerialNumber, "EquipmentDataStruct.SerialNumber 不匹配");
        Assert.AreEqual(expected.Status, actual.Status, "EquipmentDataStruct.Status 不匹配");
        Assert.AreEqual(expected.Manufacturer, actual.Manufacturer, "EquipmentDataStruct.Manufacturer 不匹配");
        Assert.AreEqual(expected.Model, actual.Model, "EquipmentDataStruct.Model 不匹配");
        Assert.AreEqual(expected.Location, actual.Location, "EquipmentDataStruct.Location 不匹配");
        Assert.AreEqual(expected.Notes, actual.Notes, "EquipmentDataStruct.Notes 不匹配");
    }

    private static void AssertProductInfoStructAreEqual(ProductInfoStruct expected, ProductInfoStruct actual) {
        Assert.AreEqual(expected.IsCreate, actual.IsCreate, "ProductInfoStruct.IsCreate 不匹配");
        Assert.AreEqual(expected.IsRead, actual.IsRead, "ProductInfoStruct.IsRead 不匹配");
        Assert.AreEqual(expected.IsUpdate, actual.IsUpdate, "ProductInfoStruct.IsUpdate 不匹配");
        Assert.AreEqual(expected.IsDelete, actual.IsDelete, "ProductInfoStruct.IsDelete 不匹配");
        Assert.AreEqual(expected.IsAddOrUpdate, actual.IsAddOrUpdate, "ProductInfoStruct.IsAddOrUpdate 不匹配");
        Assert.AreEqual(expected.IsNewFile, actual.IsNewFile, "ProductInfoStruct.IsNewFile 不匹配");
        Assert.AreEqual(expected.EquipmentId, actual.EquipmentId, "ProductInfoStruct.EquipmentId 不匹配");
        Assert.AreEqual(expected.ProductId, actual.ProductId, "ProductInfoStruct.ProductId 不匹配");
        Assert.AreEqual(expected.ProductCode, actual.ProductCode, "ProductInfoStruct.ProductCode 不匹配");
        Assert.AreEqual(expected.RecipeId, actual.RecipeId, "ProductInfoStruct.RecipeId 不匹配");
        Assert.AreEqual(expected.ProductName, actual.ProductName, "ProductInfoStruct.ProductName 不匹配");
        Assert.AreEqual(expected.Category, actual.Category, "ProductInfoStruct.Category 不匹配");
        Assert.AreEqual(expected.Notes, actual.Notes, "ProductInfoStruct.Notes 不匹配");
    }

    private static void AssertMixedDataStructAreEqual(MixedDataStruct expected, MixedDataStruct actual) {
        Assert.AreEqual(expected.IsCreate, actual.IsCreate, "MixedDataStruct.IsCreate 不匹配");
        Assert.AreEqual(expected.IsRead, actual.IsRead, "MixedDataStruct.IsRead 不匹配");
        Assert.AreEqual(expected.IsUpdate, actual.IsUpdate, "MixedDataStruct.IsUpdate 不匹配");
        Assert.AreEqual(expected.IsDelete, actual.IsDelete, "MixedDataStruct.IsDelete 不匹配");
        Assert.AreEqual(expected.IsAddOrUpdate, actual.IsAddOrUpdate, "MixedDataStruct.IsAddOrUpdate 不匹配");
        Assert.AreEqual(expected.IsNewFile, actual.IsNewFile, "MixedDataStruct.IsNewFile 不匹配");
        Assert.AreEqual(expected.IsActive, actual.IsActive, "MixedDataStruct.IsActive 不匹配");
        Assert.AreEqual(expected.IsAlarm, actual.IsAlarm, "MixedDataStruct.IsAlarm 不匹配");
        Assert.AreEqual(expected.IsOperational, actual.IsOperational, "MixedDataStruct.IsOperational 不匹配");
        Assert.AreEqual(expected.IsError, actual.IsError, "MixedDataStruct.IsError 不匹配");
        Assert.AreEqual(expected.Id, actual.Id, "MixedDataStruct.Id 不匹配");
        Assert.AreEqual(expected.Temperature, actual.Temperature, "MixedDataStruct.Temperature 不匹配");
        Assert.AreEqual(expected.Pressure, actual.Pressure, "MixedDataStruct.Pressure 不匹配");
        Assert.AreEqual(expected.Volume, actual.Volume, "MixedDataStruct.Volume 不匹配");
        Assert.AreEqual(expected.DeviceName, actual.DeviceName, "MixedDataStruct.DeviceName 不匹配");
        Assert.AreEqual(expected.ManufacturerName, actual.ManufacturerName, "MixedDataStruct.ManufacturerName 不匹配");
    }

    private static void AssertTemporaryTestAreEqual(TemporaryTest expected, TemporaryTest actual) {
        Assert.AreEqual(expected.Bool1, actual.Bool1, "TemporaryTest.Bool1 不匹配");
        Assert.AreEqual(expected.Bool1A, actual.Bool1A, "TemporaryTest.Bool1A 不匹配");
        Assert.AreEqual(expected.Bool2, actual.Bool2, "TemporaryTest.Bool2 不匹配");
        Assert.AreEqual(expected.Bool2A, actual.Bool2A, "TemporaryTest.Bool2A 不匹配");
        Assert.AreEqual(expected.Bool3, actual.Bool3, "TemporaryTest.Bool3 不匹配");
        Assert.AreEqual(expected.Bool3A, actual.Bool3A, "TemporaryTest.Bool3A 不匹配");
        Assert.AreEqual(expected.Bool4, actual.Bool4, "TemporaryTest.Bool4 不匹配");
        Assert.AreEqual(expected.Bool4A, actual.Bool4A, "TemporaryTest.Bool4A 不匹配");
        Assert.AreEqual(expected.Bool5, actual.Bool5, "TemporaryTest.Bool5 不匹配");
        Assert.AreEqual(expected.Bool5A, actual.Bool5A, "TemporaryTest.Bool5A 不匹配");
        Assert.AreEqual(expected.Bool6, actual.Bool6, "TemporaryTest.Bool6 不匹配");
        Assert.AreEqual(expected.Bool6A, actual.Bool6A, "TemporaryTest.Bool6A 不匹配");

        Assert.AreEqual(expected.Short1, actual.Short1, "TemporaryTest.Short1 不匹配");
        Assert.AreEqual(expected.Short1A, actual.Short1A, "TemporaryTest.Short1A 不匹配");
        Assert.AreEqual(expected.Short2, actual.Short2, "TemporaryTest.Short2 不匹配");
        Assert.AreEqual(expected.Short2A, actual.Short2A, "TemporaryTest.Short2A 不匹配");

        Assert.AreEqual(expected.float1, actual.float1, "TemporaryTest.float1 不匹配");
        Assert.AreEqual(expected.float1A, actual.float1A, "TemporaryTest.float1A 不匹配");
        Assert.AreEqual(expected.float2, actual.float2, "TemporaryTest.float2 不匹配");
        Assert.AreEqual(expected.float2A, actual.float2A, "TemporaryTest.float2A 不匹配");

        Assert.AreEqual(expected.double1, actual.double1, "TemporaryTest.double1 不匹配");
        Assert.AreEqual(expected.double1A, actual.double1A, "TemporaryTest.double1A 不匹配");
        Assert.AreEqual(expected.double2, actual.double2, "TemporaryTest.double2 不匹配");
        Assert.AreEqual(expected.double2A, actual.double2A, "TemporaryTest.double2A 不匹配");

        Assert.AreEqual(expected.Int1, actual.Int1, "TemporaryTest.Int1 不匹配");
        Assert.AreEqual(expected.Int1A, actual.Int1A, "TemporaryTest.Int1A 不匹配");
        Assert.AreEqual(expected.Int2, actual.Int2, "TemporaryTest.Int2 不匹配");
        Assert.AreEqual(expected.Int2A, actual.Int2A, "TemporaryTest.Int2A 不匹配");

        Assert.AreEqual(expected.String1, actual.String1, "TemporaryTest.String1 不匹配");
        Assert.AreEqual(expected.String1A, actual.String1A, "TemporaryTest.String1A 不匹配");
        Assert.AreEqual(expected.String2, actual.String2, "TemporaryTest.String2 不匹配");
        Assert.AreEqual(expected.String2A, actual.String2A, "TemporaryTest.String2A 不匹配");
    }

    #endregion

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

    public TestContext TestContext { get; set; }
}

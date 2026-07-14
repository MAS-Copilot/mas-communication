// =============================================================================
// MAS.Communication
// https://www.mas-automation.com/
//
// Copyright 2026 MAS (厦门威光) Corporation
//
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.
// =============================================================================

using MAS.Communication.OpcUaProtocol;
using Opc.Ua;

namespace MAS.CommunicationUnitTest.OpcUaProtocol;

[TestClass]
public sealed class OpcUaValueConverterTest {
    [TestMethod]
    public void ToNodeId_解析标准文本格式() {
        NodeId stringNode = OpcUaValueConverter.ToNodeId(new OpcUaNodeId("ns=2;s=Machine.Speed"));
        Assert.AreEqual(2, stringNode.NamespaceIndex);
        Assert.AreEqual("Machine.Speed", stringNode.Identifier);

        NodeId numericNode = OpcUaValueConverter.ToNodeId(new OpcUaNodeId("ns=3;i=1001"));
        Assert.AreEqual(3, numericNode.NamespaceIndex);
        Assert.AreEqual((uint)1001, numericNode.Identifier);
    }

    [TestMethod]
    public void ToNodeId_空标识抛出异常() {
        _ = Assert.ThrowsExactly<ArgumentException>(() => _ = OpcUaValueConverter.ToNodeId(new OpcUaNodeId(string.Empty)));
    }

    [TestMethod]
    public void ToOpcUaValue_携带状态码与时间戳() {
        DateTime source = new(2026, 7, 14, 8, 0, 0, DateTimeKind.Utc);
        DateTime server = new(2026, 7, 14, 8, 0, 1, DateTimeKind.Utc);
        DataValue dataValue = new(new Variant(123)) {
            StatusCode = StatusCodes.Good,
            SourceTimestamp = source,
            ServerTimestamp = server
        };

        OpcUaValue value = OpcUaValueConverter.ToOpcUaValue(new OpcUaNodeId("ns=2;s=A"), dataValue);

        Assert.AreEqual(123, value.Value);
        Assert.AreEqual(StatusCodes.Good, value.StatusCode);
        Assert.IsTrue(value.IsGood);
        Assert.AreEqual(source, value.SourceTimestamp);
        Assert.AreEqual(server, value.ServerTimestamp);
    }

    [TestMethod]
    public void ToOpcUaValue_默认时间戳映射为Null() {
        DataValue dataValue = new(new Variant(1)) { StatusCode = StatusCodes.Good };

        OpcUaValue value = OpcUaValueConverter.ToOpcUaValue(new OpcUaNodeId("ns=2;s=A"), dataValue);

        Assert.IsNull(value.SourceTimestamp);
        Assert.IsNull(value.ServerTimestamp);
    }

    [TestMethod]
    public void ConvertValue_基本类型与可空类型() {
        Assert.AreEqual(42, OpcUaValueConverter.ConvertValue<int>(42));
        Assert.AreEqual(42, OpcUaValueConverter.ConvertValue<int>((short)42));
        Assert.AreEqual(1.5, OpcUaValueConverter.ConvertValue<double>(1.5f), 0.0001);
        Assert.AreEqual("text", OpcUaValueConverter.ConvertValue<string>("text"));

        int? nullableResult = OpcUaValueConverter.ConvertValue<int?>(null);
        Assert.IsNull(nullableResult);

        int defaultResult = OpcUaValueConverter.ConvertValue<int>(null);
        Assert.AreEqual(0, defaultResult);
    }

    [TestMethod]
    public void MapNodeClass_映射节点类别() {
        Assert.AreEqual(OpcUaNodeClass.Object, OpcUaValueConverter.MapNodeClass(NodeClass.Object));
        Assert.AreEqual(OpcUaNodeClass.Variable, OpcUaValueConverter.MapNodeClass(NodeClass.Variable));
        Assert.AreEqual(OpcUaNodeClass.Method, OpcUaValueConverter.MapNodeClass(NodeClass.Method));
        Assert.AreEqual(OpcUaNodeClass.Unspecified, OpcUaValueConverter.MapNodeClass(NodeClass.Unspecified));
    }

    [TestMethod]
    public void IsBad_状态码严重级别判定() {
        Assert.IsFalse(OpcUaValueConverter.IsBad(0x00000000));
        Assert.IsFalse(OpcUaValueConverter.IsBad(0x40000000));
        Assert.IsTrue(OpcUaValueConverter.IsBad(0x80340000));
    }
}

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

namespace MAS.CommunicationUnitTest.OpcUaProtocol;

[TestClass]
public sealed class OpcUaModelsTest {
    private const uint GOOD = 0x00000000;
    private const uint UNCERTAIN = 0x40000000;
    private const uint BAD_NODE_ID_UNKNOWN = 0x80340000;

    [TestMethod]
    public void OpcUaValue_状态码判定() {
        OpcUaValue good = new(new OpcUaNodeId("ns=2;s=A"), 1, GOOD, null, null);
        OpcUaValue uncertain = new(new OpcUaNodeId("ns=2;s=B"), 1, UNCERTAIN, null, null);
        OpcUaValue bad = new(new OpcUaNodeId("ns=2;s=C"), null, BAD_NODE_ID_UNKNOWN, null, null);

        Assert.IsTrue(good.IsGood);
        Assert.IsFalse(good.IsBad);

        Assert.IsFalse(uncertain.IsGood);
        Assert.IsFalse(uncertain.IsBad);

        Assert.IsFalse(bad.IsGood);
        Assert.IsTrue(bad.IsBad);
    }

    [TestMethod]
    public void OpcUaWriteResult_状态码判定() {
        OpcUaWriteResult good = new(new OpcUaNodeId("ns=2;s=A"), GOOD);
        OpcUaWriteResult bad = new(new OpcUaNodeId("ns=2;s=B"), BAD_NODE_ID_UNKNOWN);

        Assert.IsTrue(good.IsGood);
        Assert.IsTrue(bad.IsBad);
        Assert.IsFalse(bad.IsGood);
    }

    [TestMethod]
    public void OpcUaMethodResult_状态码判定() {
        OpcUaMethodResult result = new(GOOD, [1, "abc"]);

        Assert.IsTrue(result.IsGood);
        Assert.HasCount(2, result.OutputArguments);
        Assert.AreEqual(1, result.OutputArguments[0]);
        Assert.AreEqual("abc", result.OutputArguments[1]);
    }

    [TestMethod]
    public void OpcUaNodeId_文本与隐式转换() {
        OpcUaNodeId explicitId = new("ns=3;i=1001");
        OpcUaNodeId implicitId = "ns=3;i=1001";

        Assert.AreEqual("ns=3;i=1001", explicitId.Value);
        Assert.AreEqual("ns=3;i=1001", explicitId.ToString());
        Assert.AreEqual(explicitId, implicitId);
    }

    [TestMethod]
    public void OpcUaSubscriptionOptions_默认值() {
        OpcUaSubscriptionOptions options = new();

        Assert.AreEqual(1000, options.PublishingInterval);
        Assert.AreEqual(10u, options.KeepAliveCount);
        Assert.AreEqual(30u, options.LifetimeCount);
        Assert.AreEqual(0u, options.MaxNotificationsPerPublish);
        Assert.IsTrue(options.PublishingEnabled);
    }

    [TestMethod]
    public void OpcUaMonitoredItem_默认值与节点标识() {
        OpcUaMonitoredItem item = new("ns=2;s=Machine.Speed") {
            SamplingInterval = 500,
            QueueSize = 5,
            DiscardOldest = false
        };

        Assert.AreEqual("ns=2;s=Machine.Speed", item.NodeId.Value);
        Assert.AreEqual(500, item.SamplingInterval);
        Assert.AreEqual(5u, item.QueueSize);
        Assert.IsFalse(item.DiscardOldest);

        OpcUaMonitoredItem defaults = new("ns=2;s=X");
        Assert.AreEqual(1000, defaults.SamplingInterval);
        Assert.AreEqual(1u, defaults.QueueSize);
        Assert.IsTrue(defaults.DiscardOldest);
    }
}

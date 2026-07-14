// =============================================================================
// MAS.Communication
// https://www.mas-automation.com/
//
// Copyright 2026 MAS (厦门威光) Corporation
//
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.
// =============================================================================

namespace MAS.Communication.OpcUaProtocol;

/// <summary>
/// OPC UA 订阅监控项
/// </summary>
/// <remarks>
/// 初始化 <see cref="OpcUaMonitoredItem"/> 新实例
/// </remarks>
/// <param name="nodeId">要监控的节点标识</param>
public sealed class OpcUaMonitoredItem(OpcUaNodeId nodeId) {

    /// <summary>
    /// 获取要监控的节点标识
    /// </summary>
    public OpcUaNodeId NodeId { get; } = nodeId;

    /// <summary>
    /// 获取采样间隔（毫秒），默认 1000
    /// </summary>
    public double SamplingInterval { get; init; } = 1000;

    /// <summary>
    /// 获取服务端队列深度，默认 1
    /// </summary>
    public uint QueueSize { get; init; } = 1;

    /// <summary>
    /// 获取一个值，指示队列满时是否丢弃最旧的数据，默认 <see langword="true"/>
    /// </summary>
    public bool DiscardOldest { get; init; } = true;
}

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
/// OPC UA 浏览结果节点
/// </summary>
/// <param name="NodeId">目标节点标识</param>
/// <param name="BrowseName">浏览名称</param>
/// <param name="DisplayName">显示名称</param>
/// <param name="NodeClass">节点类别</param>
/// <param name="IsForward">引用方向是否为正向</param>
public sealed record OpcUaBrowseNode(
    OpcUaNodeId NodeId,
    string BrowseName,
    string DisplayName,
    OpcUaNodeClass NodeClass,
    bool IsForward);

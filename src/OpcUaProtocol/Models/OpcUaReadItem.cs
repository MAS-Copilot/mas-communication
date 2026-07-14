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
/// OPC UA 批量读取项
/// </summary>
/// <param name="NodeId">要读取的节点标识</param>
public sealed record OpcUaReadItem(OpcUaNodeId NodeId);

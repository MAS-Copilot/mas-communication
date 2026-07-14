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
/// OPC UA 批量写入项
/// </summary>
/// <param name="NodeId">要写入的节点标识</param>
/// <param name="Value">要写入的值</param>
public sealed record OpcUaWriteItem(OpcUaNodeId NodeId, object? Value);

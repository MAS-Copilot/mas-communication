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
/// OPC UA 批量写入的逐项结果
/// </summary>
/// <remarks>批量写入允许部分成功、部分失败，因此每一项都会返回独立的状态码</remarks>
/// <param name="NodeId">写入的节点标识</param>
/// <param name="StatusCode">OPC UA 状态码（原始 32 位无符号整数）</param>
public sealed record OpcUaWriteResult(OpcUaNodeId NodeId, uint StatusCode) {
    /// <summary>
    /// 获取一个值，指示该状态码是否表示“良好（Good）”
    /// </summary>
    public bool IsGood => (StatusCode & 0xC0000000u) == 0u;

    /// <summary>
    /// 获取一个值，指示该状态码是否表示“错误（Bad）”
    /// </summary>
    public bool IsBad => (StatusCode & 0x80000000u) != 0u;
}

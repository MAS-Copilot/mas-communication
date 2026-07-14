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
/// OPC UA 读取结果
/// </summary>
/// <remarks>
/// OPC UA 中“请求成功”不代表每个节点都读取成功，因此结果必须携带逐项状态码与时间戳，
/// 以便调用方在批量读取时区分部分成功、部分失败的情况
/// </remarks>
/// <param name="NodeId">节点标识</param>
/// <param name="Value">读取到的值；读取失败时通常为 <see langword="null"/></param>
/// <param name="StatusCode">OPC UA 状态码（原始 32 位无符号整数）</param>
/// <param name="SourceTimestamp">数据源时间戳</param>
/// <param name="ServerTimestamp">服务器时间戳</param>
public sealed record OpcUaValue(
    OpcUaNodeId NodeId,
    object? Value,
    uint StatusCode,
    DateTime? SourceTimestamp,
    DateTime? ServerTimestamp) {
    /// <summary>
    /// 获取一个值，指示该状态码是否表示“良好（Good）”
    /// </summary>
    public bool IsGood => (StatusCode & 0xC0000000u) == 0u;

    /// <summary>
    /// 获取一个值，指示该状态码是否表示“错误（Bad）”
    /// </summary>
    public bool IsBad => (StatusCode & 0x80000000u) != 0u;
}

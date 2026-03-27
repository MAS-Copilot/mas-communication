// =============================================================================
// MAS.Communication
// https://www.mas-automation.com/
//
// Copyright 2026 MAS (厦门威光) Corporation
//
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.
// =============================================================================

namespace MAS.Communication.ModbusProtocol;

/// <summary>
/// 32/64位数据的“寄存器字序”（两个/四个寄存器的顺序）
/// </summary>
public enum ModbusWordOrder {
    /// <summary>
    /// 不交换（ABCD）
    /// </summary>
    Normal = 0,

    /// <summary>
    /// 按 16 位寄存器交换（CDAB）
    /// </summary>
    Swap = 1
}

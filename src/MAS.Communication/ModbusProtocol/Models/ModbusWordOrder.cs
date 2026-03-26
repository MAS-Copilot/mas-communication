// =============================================================================
// Professional Automation Equipment Manufacturer.
// Website:       https://www.mas-automation.com/
//
// Copyright (c) MAS(厦门威光) Corporation. All rights reserved.
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

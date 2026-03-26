// =============================================================================
// Professional Automation Equipment Manufacturer.
// Website:       https://www.mas-automation.com/
//
// Copyright (c) MAS(厦门威光) Corporation. All rights reserved.
// =============================================================================

namespace MAS.Communication.ModbusProtocol;

/// <summary>
/// 16 位寄存器内部的字节顺序枚举
/// </summary>
public enum ModbusByteOrder {
    /// <summary>
    /// 大端模式：高位字节在前，低位字节在后
    /// </summary>
    BigEndian = 0,

    /// <summary>
    /// 小端模式：低位字节在前，高位字节在后
    /// </summary>
    LittleEndian = 1
}

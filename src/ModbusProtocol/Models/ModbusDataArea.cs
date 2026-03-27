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
/// Modbus 数据区域类型枚举
/// </summary>
public enum ModbusDataArea {
    /// <summary>
    /// 0x: 线圈，读写位变量，FC01/05/15
    /// </summary>
    Coils = 0,

    /// <summary>
    /// 1x: 离散输入，只读位变量，FC02
    /// </summary>
    DiscreteInputs = 1,

    /// <summary>
    /// 4x: 保持寄存器，读写16位寄存器，FC03/06/16
    /// </summary>
    HoldingRegisters = 2,

    /// <summary>
    /// 3x: 输入寄存器，只读16位寄存器，FC04
    /// </summary>
    InputRegisters = 3
}

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
/// Modbus TCP 协议常量定义
/// </summary>
internal static class ModbusTcpConstants {
    /// <summary>
    /// 协议标识符（Protocol Identifier）
    /// </summary>
    public const ushort PROTOCOL_ID = 0;

    /// <summary>
    /// 功能码：读取线圈（Read Coils），对应 FC01
    /// </summary>
    /// <remarks>用于读取从站设备的可读写位变量（布尔值）</remarks>
    public const byte FC_READ_COILS = 0x01;

    /// <summary>
    /// 功能码：读取离散输入（Read Discrete Inputs），对应 FC02
    /// </summary>
    /// <remarks>用于读取从站设备的只读位变量（如开关量输入状态）</remarks>
    public const byte FC_READ_DISCRETE_INPUTS = 0x02;

    /// <summary>
    /// 功能码：读取保持寄存器（Read Holding Registers）
    /// </summary>
    /// <remarks>对应 FC03，用于从设备读取 16 位寄存器数组</remarks>
    public const byte FC_READ_HOLDING_REGISTERS = 0x03;

    /// <summary>
    /// 功能码：读取输入寄存器（Read Input Registers）
    /// </summary>
    /// <remarks>对应 FC04，用于从设备读取只读模拟量输入等数据</remarks>
    public const byte FC_READ_INPUT_REGISTERS = 0x04;

    /// <summary>
    /// 功能码：写单个线圈（Write Single Coil），对应 FC05
    /// </summary>
    /// <remarks>用于将单个线圈（布尔输出）置位（0xFF00）或复位（0x0000）</remarks>
    public const byte FC_WRITE_SINGLE_COIL = 0x05;

    /// <summary>
    /// 功能码：写单个保持寄存器（Write Single Holding Register），对应 FC06
    /// </summary>
    /// <remarks>用于向从站设备写入一个16位寄存器的值</remarks>
    public const byte FC_WRITE_SINGLE_REGISTER = 0x06;

    /// <summary>
    /// 功能码：写多个保持寄存器（Write Multiple Holding Registers），对应 FC16
    /// </summary>
    /// <remarks>用于批量写入多个16位寄存器，提高通信效率</remarks>
    public const byte FC_WRITE_MULTIPLE_REGISTERS = 0x10;

    /// <summary>
    /// 功能码：写多个线圈（Write Multiple Coils），对应 FC15
    /// </summary>
    /// <remarks>用于批量设置多个线圈的开关状态，提高通信效率</remarks>
    public const byte FC_WRITE_MULTIPLE_COILS = 0x0F;

    /// <summary>
    /// 异常功能码掩码（Exception Mask）
    /// </summary>
    /// <remarks>当响应功能码的最高位被置位（即原功能码 | 0x80）时，表示请求出错</remarks>
    public const byte EXCEPTION_MASK = 0x80;

    /// <summary>
    /// 每次请求最大可读取的寄存器数量（按16位寄存器计）
    /// </summary>
    /// <remarks>根据 Modbus 规范，单次最多读取 125 个保持/输入寄存器（共 250 字节）</remarks>
    public const int MAX_READ_REGISTERS_PER_REQUEST = 125;

    /// <summary>
    /// 每次请求最大可读取的位（bit）数量
    /// </summary>
    /// <remarks>根据 Modbus 协议规范，单次最多读取 2000 个线圈或离散输入。超过此值需分批读取。</remarks>
    public const int MAX_READ_BITS_PER_REQUEST = 2000;

    /// <summary>
    /// 单次请求最大可写入的寄存器数量
    /// </summary>
    /// <remarks>根据 Modbus 协议规范，最多支持连续写入 123 个寄存器（受限于报文长度限制）。超过此数量需分批发送</remarks>
    public const int MAX_WRITE_REGISTERS_PER_REQUEST = 123;

    /// <summary>
    /// 单次请求最大可写入的线圈数量
    /// </summary>
    /// <remarks>根据 Modbus 协议规范，最多支持连续写入 1968 个位（受限于报文长度和字节计数限制）。超过此数量需分批发送。</remarks>
    public const int MAX_WRITE_BITS_PER_REQUEST = 1968;
}

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
/// Modbus 通信配置接口
/// </summary>
public interface IModbusCommunicationConfig : ICommunicationConfig {
    /// <summary>
    /// 获取或设置 Modbus TCP 通信端口
    /// </summary>
    /// <remarks>默认值为 502（标准 Modbus 端口）</remarks>
    short Port { get; set; }

    /// <summary>
    /// 获取或设置默认从站地址（Unit ID），取值范围通常为 1~247
    /// </summary>
    byte UnitId { get; set; }

    /// <summary>
    /// 获取或设置字节序（Byte Order）
    /// </summary>
    /// <remarks>用于控制寄存器内字节的排列方式</remarks>
    ModbusByteOrder ByteOrder { get; set; }

    /// <summary>
    /// 获取或设置字词序（Word Order）
    /// </summary>
    /// <remarks>用于控制多寄存器数据（如浮点数、长整型）中寄存器的排列顺序</remarks>
    ModbusWordOrder WordOrder { get; set; }

    /// <summary>
    /// 获取或设置是否使用“1-based”地址基准
    /// </summary>
    /// <remarks>
    /// 示例：
    /// <para>    
    /// - 若手册标注地址为 40001，对应协议实际地址为 0（0-based）
    /// </para>
    /// <para>
    /// - 启用 UseOneBasedAddress = true：调用时传 40001，内部自动转为 0
    /// </para>
    /// <para>
    /// - 禁用 UseOneBasedAddress = false：调用时必须传 0
    /// </para>
    /// </remarks>
    bool UseOneBasedAddress { get; set; }
}

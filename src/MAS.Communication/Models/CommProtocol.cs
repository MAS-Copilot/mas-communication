// =============================================================================
// Professional Automation Equipment Manufacturer.
// Website:       https://www.mas-automation.com/
//
// Copyright (c) MAS(厦门威光) Corporation. All rights reserved.
// =============================================================================

namespace MAS.Communication;

/// <summary>
/// 通讯协议枚举
/// </summary>
public enum CommProtocol {
    /// <summary>
    /// 西门子 S7 协议
    /// </summary>
    S7,

    /// <summary>
    /// 三菱 MC协议
    /// </summary>
    MC,

    /// <summary>
    /// Modbus TCP 协议
    /// </summary>
    ModbusTcp
}

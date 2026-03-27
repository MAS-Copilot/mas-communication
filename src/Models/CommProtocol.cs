// =============================================================================
// MAS.Communication
// https://www.mas-automation.com/
//
// Copyright 2026 MAS (厦门威光) Corporation
//
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.
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

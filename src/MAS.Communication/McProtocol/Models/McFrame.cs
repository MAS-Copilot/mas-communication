// =============================================================================
// Professional Automation Equipment Manufacturer.
// Website:       https://www.mas-automation.com/
//
// Copyright (c) MAS(厦门威光) Corporation. All rights reserved.
// =============================================================================

namespace MAS.Communication.McProtocol;

/// <summary>
/// 三菱 MC 协议帧类型
/// </summary>
public enum McFrame {
    /// <summary>
    /// 1E 帧（早期格式，报文短，功能受限）
    /// </summary>
    MC1E = 4,

    /// <summary>
    /// 3E 帧（常用格式，支持以太网通信）
    /// </summary>
    MC3E = 11,

    /// <summary>
    /// 4E 帧（扩展格式，支持更大数据长度）
    /// </summary>
    MC4E = 15
}

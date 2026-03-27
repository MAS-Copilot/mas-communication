// =============================================================================
// MAS.Communication
// https://www.mas-automation.com/
//
// Copyright 2026 MAS (厦门威光) Corporation
//
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.
// =============================================================================

namespace MAS.Communication.S7Protocol;

/// <summary>
/// S7 通讯参数配置接口
/// </summary>
public interface IS7CommunicationConfig : ICommunicationConfig {
    /// <summary>
    /// 获取或设置型号
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// 获取或设置机架
    /// </summary>
    public short Rack { get; set; }

    /// <summary>
    /// 获取或设置插槽
    /// </summary>
    public short Slot { get; set; }
}

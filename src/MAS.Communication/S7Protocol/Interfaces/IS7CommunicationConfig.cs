// =============================================================================
// Professional Automation Equipment Manufacturer.
// Website:       https://www.mas-automation.com/
//
// Copyright (c) MAS(厦门威光) Corporation. All rights reserved.
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

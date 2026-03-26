// =============================================================================
// Professional Automation Equipment Manufacturer.
// Website:       https://www.mas-automation.com/
//
// Copyright (c) MAS(厦门威光) Corporation. All rights reserved.
// =============================================================================

namespace MAS.Communication.McProtocol;

/// <summary>
/// MC 协议参数配置接口
/// </summary>
public interface IMcCommunicationConfig : ICommunicationConfig {
    /// <summary>
    /// 获取或设置协议帧
    /// </summary>
    public McFrame ProtocolFrame { get; set; }

    /// <summary>
    /// 获取或设置端口
    /// </summary>
    public int Port { get; set; }
}

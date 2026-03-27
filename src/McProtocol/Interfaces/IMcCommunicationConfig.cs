// =============================================================================
// MAS.Communication
// https://www.mas-automation.com/
//
// Copyright 2026 MAS (厦门威光) Corporation
//
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.
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

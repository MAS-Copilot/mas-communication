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
/// 协议实例创建事件参数
/// </summary>
public sealed class ProtocolCreatedEventArgs(IProtocol protocol) : EventArgs {
    /// <summary>
    /// 获取该实例标识
    /// </summary>
    public string InstanceId => Config.GetInstanceKey();

    /// <summary>
    /// 获取该实例的通讯配置
    /// </summary>
    public ICommunicationConfig Config => Protocol.Configuration;

    /// <summary>
    /// 获取该协议实例
    /// </summary>
    public IProtocol Protocol { get; } = protocol;
}

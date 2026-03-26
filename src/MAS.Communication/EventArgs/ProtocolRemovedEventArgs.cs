// =============================================================================
// Professional Automation Equipment Manufacturer.
// Website:       https://www.mas-automation.com/
//
// Copyright (c) MAS(厦门威光) Corporation. All rights reserved.
// =============================================================================

namespace MAS.Communication;

/// <summary>
/// 协议实例移除事件参数
/// </summary>
public sealed class ProtocolRemovedEventArgs(string instanceId) : EventArgs {
    /// <summary>
    /// 获取该实例标识
    /// </summary>
    public string InstanceId { get; } = instanceId;
}

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
/// 协议实例移除事件参数
/// </summary>
public sealed class ProtocolRemovedEventArgs(string instanceId) : EventArgs {
    /// <summary>
    /// 获取该实例标识
    /// </summary>
    public string InstanceId { get; } = instanceId;
}

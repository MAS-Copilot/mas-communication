// =============================================================================
// MAS.Communication
// https://www.mas-automation.com/
//
// Copyright 2026 MAS (厦门威光) Corporation
//
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.
// =============================================================================

namespace MAS.Communication.OpcUaProtocol;

/// <summary>
/// OPC UA 订阅参数
/// </summary>
public sealed class OpcUaSubscriptionOptions {
    /// <summary>
    /// 获取或设置发布间隔（毫秒），默认 1000
    /// </summary>
    public double PublishingInterval { get; set; } = 1000;

    /// <summary>
    /// 获取或设置保活计数，默认 10
    /// </summary>
    public uint KeepAliveCount { get; set; } = 10;

    /// <summary>
    /// 获取或设置生命周期计数，默认 30
    /// </summary>
    public uint LifetimeCount { get; set; } = 30;

    /// <summary>
    /// 获取或设置单次发布的最大通知数量，0 表示不限制
    /// </summary>
    public uint MaxNotificationsPerPublish { get; set; }

    /// <summary>
    /// 获取或设置一个值，指示订阅创建后是否立即启用发布，默认 <see langword="true"/>
    /// </summary>
    public bool PublishingEnabled { get; set; } = true;
}

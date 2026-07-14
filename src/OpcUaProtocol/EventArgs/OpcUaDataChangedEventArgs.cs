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
/// OPC UA 订阅数据变化事件参数
/// </summary>
/// <param name="subscriptionId">触发变化的订阅标识</param>
/// <param name="value">变化后的值</param>
public sealed class OpcUaDataChangedEventArgs(string subscriptionId, OpcUaValue value) : EventArgs {
    /// <summary>
    /// 获取触发变化的订阅标识
    /// </summary>
    public string SubscriptionId { get; } = subscriptionId;

    /// <summary>
    /// 获取变化后的值
    /// </summary>
    public OpcUaValue Value { get; } = value;
}

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
/// OPC UA 节点标识
/// </summary>
/// <remarks>
/// 使用标准文本格式表示节点，例如：
/// <list type="bullet">
/// <item><description><c>ns=2;s=Machine.Speed</c>（字符串标识）</description></item>
/// <item><description><c>ns=3;i=1001</c>（数值标识）</description></item>
/// <item><description><c>i=2258</c>（默认命名空间 0）</description></item>
/// </list>
/// 该类型是对官方 SDK NodeId 的轻量封装，避免向公共 API 泄漏 SDK 类型
/// </remarks>
/// <param name="Value">节点标识文本</param>
public readonly record struct OpcUaNodeId(string Value) {
    /// <summary>
    /// 返回节点标识文本
    /// </summary>
    /// <returns>节点标识文本</returns>
    public override string ToString() {
        return Value;
    }

    /// <summary>
    /// 将字符串隐式转换为 <see cref="OpcUaNodeId"/>
    /// </summary>
    /// <param name="value">节点标识文本</param>
#pragma warning disable MAS201 // 隐式转换运算符由编译器生成 op_Implicit 方法名，无法遵循 PascalCase 命名
    public static implicit operator OpcUaNodeId(string value) {
        return new OpcUaNodeId(value);
    }
#pragma warning restore MAS201
}

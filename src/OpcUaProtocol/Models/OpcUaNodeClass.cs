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
/// OPC UA 节点类别
/// </summary>
/// <remarks>这是对 OPC UA 规范中 NodeClass 的轻量映射，避免向公共 API 泄漏官方 SDK 类型</remarks>
public enum OpcUaNodeClass {
    /// <summary>
    /// 未指定
    /// </summary>
    Unspecified = 0,

    /// <summary>
    /// 对象节点
    /// </summary>
    Object = 1,

    /// <summary>
    /// 变量节点
    /// </summary>
    Variable = 2,

    /// <summary>
    /// 方法节点
    /// </summary>
    Method = 4,

    /// <summary>
    /// 对象类型节点
    /// </summary>
    ObjectType = 8,

    /// <summary>
    /// 变量类型节点
    /// </summary>
    VariableType = 16,

    /// <summary>
    /// 引用类型节点
    /// </summary>
    ReferenceType = 32,

    /// <summary>
    /// 数据类型节点
    /// </summary>
    DataType = 64,

    /// <summary>
    /// 视图节点
    /// </summary>
    View = 128
}

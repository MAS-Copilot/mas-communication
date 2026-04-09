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
/// 固定长度字符串字段特性（按字节长度）
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public sealed class FixedStringAttribute(int length) : Attribute {
    /// <summary> 
    /// 获取字符串字节长度 
    /// </summary>
    public int Length { get; } = length;

    /// <summary>
    /// 获取或设置编码方式
    /// </summary>
    /// <remarks>默认值： "ASCII" </remarks>
    public string EncodingName { get; set;  } = "ASCII";

    /// <summary>
    /// 获取或设置填充字符
    /// </summary>
    /// <remarks>默认值： '\0'  </remarks>
    public char PaddingChar { get; set; } = '\0';

    /// <summary>
    /// 获取或设置是否去除末尾填充
    /// </summary>
    /// <remarks>默认值： true </remarks>
    public bool TrimEndPadding { get; set; } = true;
}

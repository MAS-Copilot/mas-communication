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
    /// 获取编码方式，默认 ASCII 
    /// </summary>
    public string EncodingName { get; init; } = "ASCII";

    /// <summary>
    /// 获取填充字符，默认空字符 '\0' 
    /// </summary>
    public char PaddingChar { get; init; } = '\0';

    /// <summary>
    /// 获取是否去除末尾填充，默认 true 
    /// </summary>
    public bool TrimEndPadding { get; init; } = true;
}

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
/// 写数据错误异常
/// </summary>
public class WriteErrorException : Exception {
    /// <summary>
    /// 初始化 <see cref="WriteErrorException"/> 新实例
    /// </summary>
    /// <param name="message">异常消息</param>
    public WriteErrorException(string message) : base(message) { }

    /// <summary>
    /// 初始化 <see cref="WriteErrorException"/> 新实例
    /// </summary>
    /// <param name="message">异常消息</param>
    /// <param name="innerException">内部异常</param>
    public WriteErrorException(string message, Exception innerException) : base(message, innerException) { }
}

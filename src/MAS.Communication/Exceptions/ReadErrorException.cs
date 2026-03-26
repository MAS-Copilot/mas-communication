// =============================================================================
// Professional Automation Equipment Manufacturer.
// Website:       https://www.mas-automation.com/
//
// Copyright (c) MAS(厦门威光) Corporation. All rights reserved.
// =============================================================================

namespace MAS.Communication;

/// <summary>
/// 读取数据错误异常
/// </summary>
public class ReadErrorException : Exception {
    /// <summary>
    /// 初始化 <see cref="ReadErrorException"/> 新实例
    /// </summary>
    /// <param name="message">异常消息</param>
    public ReadErrorException(string message) : base(message) { }

    /// <summary>
    /// 初始化 <see cref="ReadErrorException"/> 新实例
    /// </summary>
    /// <param name="message">异常消息</param>
    /// <param name="innerException">内部异常</param>
    public ReadErrorException(string message, Exception innerException) : base(message, innerException) { }
}

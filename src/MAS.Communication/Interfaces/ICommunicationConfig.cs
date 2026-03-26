// =============================================================================
// Professional Automation Equipment Manufacturer.
// Website:       https://www.mas-automation.com/
//
// Copyright (c) MAS(厦门威光) Corporation. All rights reserved.
// =============================================================================

namespace MAS.Communication;

/// <summary>
/// 通讯协议的基本参数配置接口
/// </summary>
public interface ICommunicationConfig : ICloneable {
    /// <summary>
    /// 获取协议名称
    /// </summary>
    public string ProtocolName { get; }

    /// <summary>
    /// 获取或设置 Ip 地址
    /// </summary>
    public string Ip { get; set; }

    /// <summary>
    /// 获取或设置连接失败时的重试次数上限
    /// </summary>
    public int MaxRetries { get; set; }

    /// <summary>
    /// 获取或设置读取超时时间(ms)
    /// </summary>
    public int ReadTimeout { get; set; }

    /// <summary>
    /// 获取或设置写入超时时间（ms）
    /// </summary>
    public int WriteTimeout { get; set; }

    /// <summary>
    /// 泛型克隆方法，返回指定类型的克隆实例
    /// </summary>
    /// <typeparam name="T">克隆实例的类型，必须实现 <see cref="ICommunicationConfig"/> 接口</typeparam>
    /// <returns>克隆后的实例</returns>
    T Clone<T>() where T : ICommunicationConfig;
}

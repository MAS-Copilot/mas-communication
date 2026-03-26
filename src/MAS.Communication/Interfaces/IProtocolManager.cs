// =============================================================================
// Professional Automation Equipment Manufacturer.
// Website:       https://www.mas-automation.com/
//
// Copyright (c) MAS(厦门威光) Corporation. All rights reserved.
// =============================================================================

namespace MAS.Communication;

/// <summary>
/// 通讯协议实例管理器接口
/// </summary>
public interface IProtocolManager : IDisposable {
    /// <summary>
    /// 获取当前已缓存的协议实例数量
    /// </summary>
    int Count { get; }

    /// <summary>
    /// 当协议实例被创建时触发的事件
    /// </summary>
    public event EventHandler<ProtocolCreatedEventArgs>? ProtocolCreated;

    /// <summary>
    /// 当协议实例被移除时触发的事件
    /// </summary>
    public event EventHandler<ProtocolRemovedEventArgs>? ProtocolRemoved;

    /// <summary>
    /// 判断指定实例标识是否已存在于缓存中
    /// </summary>
    /// <param name="instanceId">实例标识</param>
    /// <returns>存在返回 true，否则返回 false</returns>
    bool Contains(string instanceId);

    /// <summary>
    /// 判断指定协议实例是否已存在于缓存中
    /// </summary>
    /// <param name="protocol">协议实例</param>
    /// <returns>存在返回 true，否则返回 false</returns>
    bool Contains(IProtocol protocol);

    /// <summary>
    /// 根据配置获取协议实例；如果实例不存在，则创建并缓存
    /// </summary>
    /// <param name="config">通讯配置实例</param>
    /// <returns>协议实例</returns>
    /// <exception cref="NotSupportedException">不支持的通讯配置类型时抛出</exception>
    /// <exception cref="ObjectDisposedException">对象已被释放时抛出</exception>
    IProtocol GetOrCreate(ICommunicationConfig config);

    /// <summary>
    /// 根据配置获取指定类型的协议实例；如果实例不存在，则创建并缓存
    /// </summary>
    /// <typeparam name="TProtocol">协议类型</typeparam>
    /// <param name="config">通讯配置实例</param>
    /// <returns>指定类型的协议实例</returns>
    /// <exception cref="InvalidCastException">创建的协议实例与指定类型不匹配时抛出</exception>
    /// <exception cref="NotSupportedException">不支持的通讯配置类型时抛出</exception>
    /// <exception cref="ObjectDisposedException">对象已被释放时抛出</exception>
    TProtocol GetOrCreate<TProtocol>(ICommunicationConfig config) where TProtocol : class, IProtocol;

    /// <summary>
    /// 根据实例标识获取已缓存的协议实例
    /// </summary>
    /// <param name="instanceId">实例标识</param>
    /// <returns>协议实例；不存在时返回 null</returns>
    IProtocol? Get(string instanceId);

    /// <summary>
    /// 根据实例标识获取已缓存的指定类型协议实例
    /// </summary>
    /// <typeparam name="TProtocol">协议类型</typeparam>
    /// <param name="instanceId">实例标识</param>
    /// <returns>指定类型协议实例；不存在或类型不匹配时返回 null</returns>
    TProtocol? Get<TProtocol>(string instanceId) where TProtocol : class, IProtocol;

    /// <summary>
    /// 获取当前所有实例标识
    /// </summary>
    /// <returns>实例标识集合快照</returns>
    IReadOnlyCollection<string> GetInstanceIds();

    /// <summary>
    /// 移除并释放指定标识的协议实例
    /// </summary>
    /// <param name="instanceId">实例标识</param>
    /// <returns>移除成功返回 true，否则返回 false</returns>
    /// <exception cref="ObjectDisposedException">对象已被释放时抛出</exception>
    bool Remove(string instanceId);

    /// <summary>
    /// 移除并释放指定实例
    /// </summary>
    /// <param name="protocol">通讯实例</param>
    /// <returns>移除成功返回 true，否则返回 false</returns>
    /// <exception cref="ObjectDisposedException">对象已被释放时抛出</exception>
    bool Remove(IProtocol protocol);
}

// =============================================================================
// MAS.Communication
// https://www.mas-automation.com/
//
// Copyright 2026 MAS (厦门威光) Corporation
//
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.
// =============================================================================

using Microsoft.Extensions.DependencyInjection;

namespace MAS.Communication;

/// <summary>
/// 提供扩展方法，用于在 <see cref="IServiceCollection"/> 中注册与通讯相关的服务
/// </summary>
public static class ServiceCollectionExtensions {
    /// <summary>
    /// 在指定的 <see cref="IServiceCollection"/> 中注册所有通讯相关的服务
    /// </summary>
    /// <remarks>
    /// <list>
    /// <item><description><see cref="IProtocolManager"/>：通讯协议实例管理</description></item>
    /// </list>
    /// </remarks>
    /// <param name="services">要添加服务的 <see cref="IServiceCollection"/> 实例</param>
    /// <returns>已注册的服务集合</returns>
    public static IServiceCollection AddCommunication(this IServiceCollection services) {
        return services.AddSingleton<IProtocolManager, ProtocolManager>();
    }
}

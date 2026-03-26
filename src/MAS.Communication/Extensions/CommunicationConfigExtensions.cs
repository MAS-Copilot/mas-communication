// =============================================================================
// Professional Automation Equipment Manufacturer.
// Website:       https://www.mas-automation.com/
//
// Copyright (c) MAS(厦门威光) Corporation. All rights reserved.
// =============================================================================

using MAS.Communication.McProtocol;
using MAS.Communication.ModbusProtocol;
using MAS.Communication.S7Protocol;

namespace MAS.Communication;

/// <summary>
/// 通讯配置扩展方法
/// </summary>
public static class CommunicationConfigExtensions {
    /// <summary>
    /// 获取通讯实例键
    /// </summary>
    /// <param name="config">通讯配置</param>
    /// <returns>唯一实例键</returns>
    /// <exception cref="NotSupportedException"></exception>
    public static string GetInstanceKey(this ICommunicationConfig config) {
        return config switch {
            IS7CommunicationConfig s7 => $"{s7.Type}-{s7.Ip}-{s7.Rack}-{s7.Slot}",
            IMcCommunicationConfig mc => $"{mc.ProtocolName}-{mc.ProtocolFrame}-{mc.Ip}-{mc.Port}",
            IModbusCommunicationConfig mb => $"{mb.ProtocolName}-{mb.Ip}-{mb.Port}-{mb.UnitId}",
            _ => throw new NotSupportedException($"Unsupported communication config type: {config.GetType().Name}")
        };
    }
}

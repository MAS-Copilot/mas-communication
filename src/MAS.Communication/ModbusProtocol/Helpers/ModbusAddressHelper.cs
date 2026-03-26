// =============================================================================
// Professional Automation Equipment Manufacturer.
// Website:       https://www.mas-automation.com/
//
// Copyright (c) MAS(厦门威光) Corporation. All rights reserved.
// =============================================================================

namespace MAS.Communication.ModbusProtocol;

/// <summary>
/// Modbus 地址辅助类，提供地址标准化处理功能
/// </summary>
/// <remarks>将输入的地址根据配置的地址基准模式（0-based 或 1-based）转换为协议层使用的实际地址</remarks>
internal static class ModbusAddressHelper {
    /// <summary>
    /// 将外部地址规范化为协议层使用的 0-based 地址
    /// </summary>
    /// <remarks>若启用 one-based 模式，则自动减 1；否则直接返回原地址</remarks>
    /// <param name="address">原始地址</param>
    /// <param name="useOneBasedAddress">是否启用 1-based 地址模式（即地址从 1 开始计数）</param>
    /// <returns>返回协议实际使用的 0-based 地址</returns>
    /// <exception cref="ReadErrorException"></exception>
    public static ushort NormalizeAddress(ushort address, bool useOneBasedAddress) {
        if (!useOneBasedAddress) {
            return address;
        }

        if (address == 0) {
            throw new ReadErrorException("One-based address cannot be 0.");
        }

        return (ushort)(address - 1);
    }
}

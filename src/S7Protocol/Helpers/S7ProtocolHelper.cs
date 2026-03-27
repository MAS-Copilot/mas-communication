// =============================================================================
// MAS.Communication
// https://www.mas-automation.com/
//
// Copyright 2026 MAS (厦门威光) Corporation
//
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.
// =============================================================================

using S7.Net;

namespace MAS.Communication.S7Protocol;

/// <summary>
/// Helper class for S7Protocols
/// </summary>
public static class S7ProtocolHelper {
    /// <summary>
    /// 将字符串解析为 CpuType 枚举
    /// </summary>
    /// <param name="cpuType">CPU类型</param>
    /// <returns>CpuType枚举</returns>
    /// <exception cref="PlcException">CPU 不支持时抛出此异常</exception>
    public static CpuType ParseCpuType(string cpuType) {
        return cpuType switch {
            "S7-200" => CpuType.S7200,
            "S7-300" => CpuType.S7300,
            "S7-400" => CpuType.S7400,
            "S7-1200" => CpuType.S71200,
            "S7-1500" => CpuType.S71500,
            _ => throw new PlcException(ErrorCode.WrongCPU_Type)
        };
    }
}

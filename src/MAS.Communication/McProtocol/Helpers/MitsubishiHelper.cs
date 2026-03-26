// =============================================================================
// Professional Automation Equipment Manufacturer.
// Website:       https://www.mas-automation.com/
//
// Copyright (c) MAS(厦门威光) Corporation. All rights reserved.
// =============================================================================

namespace MAS.Communication.McProtocol;

/// <summary>
/// Helper class for Mitsubishi
/// </summary>
internal static class MitsubishiHelper {
    /// <summary>
    /// 将字符串解析为McFrame枚举
    /// </summary>
    /// <exception cref="ArgumentException"></exception>
    public static McFrame ParseMcFrame(string frameType) {
        return frameType switch {
            "MC1E" => McFrame.MC1E,
            "MC3E" => McFrame.MC3E,
            "MC4E" => McFrame.MC4E,
            _ => throw new ArgumentException($"Unsupported frame type: {frameType}"),
        };
    }

    /// <summary>
    /// 将字符串解析为PlcDeviceType枚举
    /// </summary>
    /// <exception cref="ArgumentException"></exception>
    public static PlcDeviceType ParsePlcDeviceType(string deviceType) {
        return deviceType.ToUpper() switch {
            "M" => PlcDeviceType.M,
            "SM" => PlcDeviceType.SM,
            "L" => PlcDeviceType.L,
            "F" => PlcDeviceType.F,
            "V" => PlcDeviceType.V,
            "S" => PlcDeviceType.S,
            "X" => PlcDeviceType.X,
            "Y" => PlcDeviceType.Y,
            "B" => PlcDeviceType.B,
            "SB" => PlcDeviceType.SB,
            "DX" => PlcDeviceType.DX,
            "DY" => PlcDeviceType.DY,
            "D" => PlcDeviceType.D,
            "SD" => PlcDeviceType.SD,
            "R" => PlcDeviceType.R,
            "ZR" => PlcDeviceType.ZR,
            "W" => PlcDeviceType.W,
            "SW" => PlcDeviceType.SW,
            "TC" => PlcDeviceType.TC,
            "TS" => PlcDeviceType.TS,
            "TN" => PlcDeviceType.TN,
            "CC" => PlcDeviceType.CC,
            "CS" => PlcDeviceType.CS,
            "CN" => PlcDeviceType.CN,
            "SC" => PlcDeviceType.SC,
            "SS" => PlcDeviceType.SS,
            "SN" => PlcDeviceType.SN,
            "Z" => PlcDeviceType.Z,
            "TT" => PlcDeviceType.TT,
            "TM" => PlcDeviceType.TM,
            "CT" => PlcDeviceType.CT,
            "CM" => PlcDeviceType.CM,
            "A" => PlcDeviceType.A,
            _ => throw new ArgumentException($"Unsupported device type: {deviceType}"),
        };
    }

    /// <summary>
    /// 检查响应数据是否错误（即不符合预期）
    /// </summary>
    /// <param name="response">响应数据</param>
    /// <param name="minLength">最小长度</param>
    /// <param name="frameType">协议类型</param>
    /// <returns>如果响应数据不正确，返回 true；否则返回 false</returns>
    public static bool IsIncorrectResponse(McFrame frameType, byte[] response, int minLength) {
        if (response.Length < minLength) {
            return false;
        }

        switch (frameType) {
            case McFrame.MC1E:
                return response.Length < minLength;
            case McFrame.MC3E:
            case McFrame.MC4E:
                var btCount = new[] {
                    response[minLength - 4], response[minLength - 3]
                };
                var btCode = new[] {
                    response[minLength - 2], response[minLength - 1]
                };
                var rsCount = BitConverter.ToUInt16(btCount, 0) - 2;
                var rsCode = BitConverter.ToUInt16(btCode, 0);
                return rsCode == 0 && rsCount != (response.Length - minLength);
            default:
                throw new Exception("Frame type not supported.");
        }
    }
}


// =============================================================================
// MAS.Communication
// https://www.mas-automation.com/
//
// Copyright 2026 MAS (厦门威光) Corporation
//
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.
// =============================================================================

using MAS.Communication;
using System.Runtime.InteropServices;

namespace MAS.CommunicationUnitTest.ModbusProtocol;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ModbusStressInnerB {
    public ushort BU16_1;
    public ushort BU16_2;
    public int BI32_1;
    public int BI32_2;
    public double BF64;

    [FixedString(20)]
    public string BTag;

    public static ModbusStressInnerB CreateRandom(Random rand, ulong salt) {
        string tag = $"B-{salt:X16}-{rand.Next(0, 9999):D4}";
        return new ModbusStressInnerB {
            BU16_1 = (ushort)rand.Next(0, ushort.MaxValue + 1),
            BU16_2 = (ushort)rand.Next(0, ushort.MaxValue + 1),
            BI32_1 = unchecked((int)(salt & 0xFFFFFFFF)),
            BI32_2 = unchecked((int)((salt >> 32) & 0xFFFFFFFF)),
            BF64 = rand.NextDouble() * 20_000_000.0 - 10_000_000.0,
            BTag = tag
        };
    }
}

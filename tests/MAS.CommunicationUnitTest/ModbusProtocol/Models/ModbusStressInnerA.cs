// =============================================================================
// Professional Automation Equipment Manufacturer.
// Website:       https://www.mas-automation.com/
//
// Copyright (c) MAS(厦门威光) Corporation. All rights reserved.
// =============================================================================

using MAS.Communication;
using System.Runtime.InteropServices;

namespace MAS.CommunicationUnitTest.ModbusProtocol;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ModbusStressInnerA {
    public bool AFlag1;
    public bool AFlag2;
    public short AS16;
    public ushort AU16;
    public int AI32;
    public float AF32;

    [FixedString(16)]
    public string AName;

    public static ModbusStressInnerA CreateRandom(Random rand, uint salt) {
        string name = $"A-{salt:X8}-{rand.Next(0, 9999):D4}";
        return new ModbusStressInnerA {
            AFlag1 = (salt & 0x10) != 0,
            AFlag2 = (salt & 0x20) != 0,
            AS16 = (short)rand.Next(short.MinValue, short.MaxValue),
            AU16 = (ushort)rand.Next(0, ushort.MaxValue + 1),
            AI32 = unchecked((int)(salt ^ (uint)rand.Next())),
            AF32 = (float)(rand.NextDouble() * 200_000.0 - 100_000.0),
            AName = name
        };
    }
}

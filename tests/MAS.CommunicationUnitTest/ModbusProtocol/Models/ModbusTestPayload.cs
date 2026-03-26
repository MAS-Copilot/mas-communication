// =============================================================================
// Professional Automation Equipment Manufacturer.
//
// Documentation: https://mas-copilot.github.io/MAS.DataMaster-Docs/
// Website:       https://www.mas-automation.com/
//
// Copyright (c) MAS(厦门威光) Corporation. All rights reserved.
// =============================================================================

using MAS.Communication;

namespace MAS.CommunicationUnitTest.ModbusProtocol;

public struct ModbusTestPayload {
    public bool Flag1;
    public bool Flag2;

    public byte B;
    public short S16;
    public ushort U16;

    public int I32;
    public uint U32;

    public float F32;
    public double F64;

    [FixedString(12)]
    public string Name;

    public ModbusInner Inner;

    public static ModbusTestPayload CreateRandom() {
        Random rand = new();

        return new ModbusTestPayload {
            Flag1 = rand.Next(2) == 0,
            Flag2 = rand.Next(2) == 0,
            B = (byte)rand.Next(0, 256),
            S16 = (short)rand.Next(short.MinValue, short.MaxValue),
            U16 = (ushort)rand.Next(0, ushort.MaxValue + 1),
            I32 = rand.Next(),
            U32 = (uint)rand.Next(),
            F32 = (float)(rand.NextDouble() * 10000.0 - 5000.0),
            F64 = rand.NextDouble() * 10000.0 - 5000.0,
            Name = $"N{rand.Next(0, 99999)}",
            Inner = new ModbusInner {
                InnerFlag = rand.Next(2) == 0,
                InnerValue = (short)rand.Next(short.MinValue, short.MaxValue)
            }
        };
    }

    public static ModbusTestPayload CreateDeterministic() {
        return new ModbusTestPayload {
            Flag1 = true,
            Flag2 = false,
            B = 0xAB,
            S16 = -1234,
            U16 = 0xBEEF,
            I32 = unchecked(0x11223344),
            U32 = 0xA1B2C3D4,
            F32 = 123.456f,
            F64 = -789.0123,
            Name = "ABC",
            Inner = new ModbusInner {
                InnerFlag = true,
                InnerValue = 0x1234
            }
        };
    }
}

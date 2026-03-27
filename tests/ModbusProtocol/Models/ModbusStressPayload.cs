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
using System.Security.Cryptography;

namespace MAS.CommunicationUnitTest.ModbusProtocol;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ModbusStressPayload {
    public bool Flag1;
    public bool Flag2;
    public bool Flag3;
    public bool Flag4;

    public byte B1;
    public byte B2;
    public byte B3;
    public byte B4;

    public short S16_1;
    public short S16_2;
    public short S16_3;
    public short S16_4;

    public ushort U16_1;
    public ushort U16_2;
    public ushort U16_3;
    public ushort U16_4;

    public int I32;
    public uint U32;

    public long I64;
    public ulong U64;

    public float F32_1;
    public float F32_2;
    public float F32_3;

    public double F64_1;
    public double F64_2;

    public int Seq;

    [FixedString(24)]
    public string Name;

    [FixedString(32)]
    public string Tag;

    public ModbusStressInnerA InnerA;
    public ModbusStressInnerB InnerB;

    public static ModbusStressPayload CreateRandom(Random rand, ModbusStressPayload? last) {
        uint r32 = GetCryptoUInt32();
        ulong r64 = (ulong)GetCryptoUInt32() << 32 | GetCryptoUInt32();

        int seq = (last?.Seq ?? 0) + 1;

        int i32 = last.HasValue ? last.Value.I32 ^ (int)r32 : unchecked((int)r32);
        long i64 = last.HasValue ? last.Value.I64 ^ (long)r64 : unchecked((long)r64);

        uint u32 = last.HasValue ? last.Value.U32 + r32 : r32;
        ulong u64 = last.HasValue ? last.Value.U64 + r64 : r64;

        float f1 = (float)(rand.NextDouble() * 2_000_000.0 - 1_000_000.0);
        float f2 = (float)(rand.NextDouble() * 2_000_000.0 - 1_000_000.0);
        float f3 = (float)(rand.NextDouble() * 2_000_000.0 - 1_000_000.0);

        double d1 = rand.NextDouble() * 2_000_000_000.0 - 1_000_000_000.0;
        double d2 = rand.NextDouble() * 2_000_000_000.0 - 1_000_000_000.0;

        string name = $"NAME-{seq:D4}-{r32:X8}-{rand.Next(0, 99999):D5}";
        string tag = $"TAG-{Guid.NewGuid():N}".ToUpperInvariant();

        return new ModbusStressPayload {
            Flag1 = (r32 & 0x1) != 0,
            Flag2 = (r32 & 0x2) != 0,
            Flag3 = (r32 & 0x4) != 0,
            Flag4 = (r32 & 0x8) != 0,

            B1 = (byte)rand.Next(0, 256),
            B2 = (byte)rand.Next(0, 256),
            B3 = (byte)rand.Next(0, 256),
            B4 = (byte)rand.Next(0, 256),

            S16_1 = (short)rand.Next(short.MinValue, short.MaxValue),
            S16_2 = (short)rand.Next(short.MinValue, short.MaxValue),
            S16_3 = (short)rand.Next(short.MinValue, short.MaxValue),
            S16_4 = (short)rand.Next(short.MinValue, short.MaxValue),

            U16_1 = (ushort)rand.Next(0, ushort.MaxValue + 1),
            U16_2 = (ushort)rand.Next(0, ushort.MaxValue + 1),
            U16_3 = (ushort)rand.Next(0, ushort.MaxValue + 1),
            U16_4 = (ushort)rand.Next(0, ushort.MaxValue + 1),

            I32 = i32,
            U32 = u32,

            I64 = i64,
            U64 = u64,

            F32_1 = f1,
            F32_2 = f2,
            F32_3 = f3,

            F64_1 = d1,
            F64_2 = d2,

            Seq = seq,

            Name = name,
            Tag = tag,

            InnerA = ModbusStressInnerA.CreateRandom(rand, r32),
            InnerB = ModbusStressInnerB.CreateRandom(rand, r64)
        };
    }

    private static uint GetCryptoUInt32() {
        Span<byte> buf = stackalloc byte[4];
        RandomNumberGenerator.Fill(buf);
        return (uint)(buf[0] | buf[1] << 8 | buf[2] << 16 | buf[3] << 24);
    }
}

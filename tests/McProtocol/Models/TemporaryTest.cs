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

namespace MAS.CommunicationUnitTest.McProtocol;

#pragma warning disable CS0649
internal struct TemporaryTest {
    public bool Bool1;
    public bool Bool2;
    public bool Bool3;
    public bool Bool4;
    public bool Bool5;
    public bool Bool6;
    public short Short1;
    public short Short2;
    public float float1;
    public float float2;
    public double double1;
    public double double2;
    public int Int1;
    public int Int2;
    [FixedString(10)]
    public string String1;
    [FixedString(20)]
    public string String2;
    public bool Bool1A;
    public bool Bool2A;
    public bool Bool3A;
    public bool Bool4A;
    public bool Bool5A;
    public bool Bool6A;
    public short Short1A;
    public short Short2A;
    public float float1A;
    public float float2A;
    public double double1A;
    public double double2A;
    public int Int1A;
    public int Int2A;
    [FixedString(10)]
    public string String1A;
    [FixedString(20)]
    public string String2A;
}
#pragma warning restore CS0649

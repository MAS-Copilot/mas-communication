// =============================================================================
// Professional Automation Equipment Manufacturer.
// Website:       https://www.mas-automation.com/
//
// Copyright (c) MAS(厦门威光) Corporation. All rights reserved.
// =============================================================================

using MAS.Communication;
using System.Runtime.InteropServices;

namespace MAS.CommunicationUnitTest.McProtocol;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct MixedDataStruct {
    public bool IsCreate;           // 是否创建(CRUD) -> D3233.0
    public bool IsRead;             // 是否读取(CRUD) -> D3233.1
    public bool IsUpdate;           // 是否更新(CRUD) -> D3233.2
    public bool IsDelete;           // 是否删除(CRUD) -> D3233.3
    public bool IsAddOrUpdate;      // 是否添加或更新(CRUD) -> D3233.4
    public bool IsNewFile;          // 是否新建文件 -> D3233.5
    public bool IsActive;           // 1 位 -> D3233.6
    public bool IsAlarm;            // 1 位 -> D3233.7
    public bool IsOperational;      // 1 位 -> D3233.8
    public bool IsError;            // 1 位 -> D3233.9
    public short Id;                // 2 字节 -> D3234, D3235
    public float Temperature;       // 4 字节 -> D3236 ~ D3239
    public double Pressure;         // 8 字节 -> D3240 ~ D3247
    public int Volume;              // 4 字节 -> D3248 ~ D3251
    [FixedString(20)]
    public string DeviceName;       // 20 字节 -> D3252 ~ D3271
    [FixedString(50)]
    public string ManufacturerName; // 50 字节 -> D3272 ~ D3321
}

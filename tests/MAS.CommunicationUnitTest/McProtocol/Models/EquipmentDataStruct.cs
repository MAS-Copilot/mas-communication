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
internal struct EquipmentDataStruct {
    public bool IsCreate;           // 是否创建(CRUD) -> D3000.0
    public bool IsRead;             // 是否读取(CRUD) -> D3000.1
    public bool IsUpdate;           // 是否更新(CRUD) -> D3000.2
    public bool IsDelete;           // 是否删除(CRUD) -> D3000.3
    public bool IsAddOrUpdate;      // 是否添加或更新(CRUD) -> D3000.4
    public bool IsNewFile;          // 是否新建文件 -> D3000.5
    [FixedString(20)]
    public string EquipmentName;    // 设备的名称 -> D3001 ~ D3010
    [FixedString(20)]
    public string EquipmentType;    // 设备类型 -> D3011 ~ D3020
    [FixedString(50)]
    public string SerialNumber;     // 设备的序列号，唯一 -> D3021 ~ D3045
    [FixedString(10)]
    public string Status;           // 设备当前的操作状态 -> D3046 ~ D3050
    [FixedString(20)]
    public string Manufacturer;     // 设备的制造商 -> D3051 ~ D3060
    [FixedString(50)]
    public string Model;            // 设备型号 -> D3061 ~ D3085
    [FixedString(50)]
    public string Location;         // 设备的安装位置 -> D3086 ~ D3110
    [FixedString(50)]
    public string Notes;            // 关于设备的额外注释或详细信息 -> D3111 ~ D3135
}

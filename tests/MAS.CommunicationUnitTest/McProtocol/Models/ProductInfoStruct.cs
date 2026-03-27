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
internal struct ProductInfoStruct {
    public bool IsCreate;           // 是否创建(CRUD) -> D3146.0
    public bool IsRead;             // 是否读取(CRUD) -> D3146.1
    public bool IsUpdate;           // 是否更新(CRUD) -> D3146.2
    public bool IsDelete;           // 是否删除(CRUD) -> D3146.3
    public bool IsAddOrUpdate;      // 是否添加或更新(CRUD) -> D3146.4
    public bool IsNewFile;          // 是否新建文件 -> D3146.5
    public short EquipmentId;       // 设备唯一标识符，外键，关联到 EquipmentData 表 -> D3147
    [FixedString(20)]
    public string ProductId;        // 产品Id，唯一 -> D3148 ~ D3157
    [FixedString(20)]
    public string ProductCode;      // 产品编码 -> D3158 ~ D3167
    [FixedString(20)]
    public string RecipeId;         // 产品配方ID -> D3168 ~ D3177
    [FixedString(20)]
    public string ProductName;      // 产品名称 -> D3178 ~ D3187
    [FixedString(20)]
    public string Category;         // 产品类别 -> D3188 ~ D3197
    [FixedString(50)]
    public string Notes;            // 额外注释或详细信息 -> D3198 ~ D3222
}

// =============================================================================
// Professional Automation Equipment Manufacturer.
// Website:       https://www.mas-automation.com/
//
// Copyright (c) MAS(厦门威光) Corporation. All rights reserved.
// =============================================================================

namespace MAS.Communication.ModbusProtocol;

/// <summary>
/// Modbus TCP 报文构建工具类，用于生成标准协议帧
/// </summary>
internal static class ModbusTcpFrameBuilder {
    /// <summary>
    /// 构建读取寄存器请求报文（FC03/FC04）
    /// </summary>
    /// <param name="transactionId">事务ID，用于匹配请求与响应</param>
    /// <param name="unitId">从站地址（Unit ID）</param>
    /// <param name="functionCode">功能码</param>
    /// <param name="startAddress">起始地址（0-based）</param>
    /// <param name="count">读取寄存器数量</param>
    /// <returns>完整的 Modbus TCP 帧字节数组</returns>
    public static byte[] BuildReadRegistersRequest(ushort transactionId, byte unitId, byte functionCode, ushort startAddress, ushort count) {
        // MBAP Length = UnitId(1) + PDU(1+2+2) = 6
        const ushort length = 6;

        byte[] frame = [
            (byte)(transactionId >> 8), // TID 高字节
            (byte)(transactionId & 0xFF),   // TID 低字节
            0,  // 协议ID高字节
            0,  // 协议ID低字节
            (length >> 8),  // 长度高字节
            (length & 0xFF),    // 长度低字节
            unitId, // 从站地址
            functionCode,   // 功能码
            (byte)(startAddress >> 8),  // 起始地址高字节
            (byte)(startAddress & 0xFF),    // 起始地址低字节
            (byte)(count >> 8), // 寄存器数量高字节
            (byte)(count & 0xFF)    // 寄存器数量低字节
        ];

        return frame;
    }

    /// <summary>
    /// 构建读取位变量请求报文（FC01/FC02）
    /// </summary>
    /// <param name="transactionId">事务ID，用于匹配请求与响应</param>
    /// <param name="unitId">从站地址（Unit ID）</param>
    /// <param name="functionCode">功能码</param>
    /// <param name="startAddress">起始地址（0-based）</param>
    /// <param name="count">读取的位数量</param>
    /// <returns>完整的 Modbus TCP 请求帧字节数组</returns>
    public static byte[] BuildReadBitsRequest(ushort transactionId, byte unitId, byte functionCode, ushort startAddress, ushort count) {
        // MBAP Length = UnitId(1) + PDU(1+2+2) = 6
        const ushort length = 6;

        byte[] frame = [
            (byte)(transactionId >> 8), // TID 高字节
            (byte)(transactionId & 0xFF),   // TID 低字节
            0,  // 协议ID高字节
            0,  // 协议ID低字节
            (length >> 8),  // 长度高字节
            (length & 0xFF),    // 长度低字节
            unitId, // 从站地址
            functionCode,   // 功能码
            (byte)(startAddress >> 8),  // 起始地址高字节
            (byte)(startAddress & 0xFF),    // 起始地址低字节
            (byte)(count >> 8), // 位数量高字节
            (byte)(count & 0xFF)    // 位数量低字节
        ];

        return frame;
    }

    /// <summary>
    /// 构建写单个保持寄存器请求报文（FC06）
    /// </summary>
    /// <param name="transactionId">事务ID，用于匹配请求与响应</param>
    /// <param name="unitId">从站地址（Unit ID）</param>
    /// <param name="address">目标寄存器地址（0-based）</param>
    /// <param name="value">要写入的16位寄存器值</param>
    /// <returns>完整的 Modbus TCP 请求帧字节数组</returns>
    public static byte[] BuildWriteSingleRegisterRequest(ushort transactionId, byte unitId, ushort address, ushort value) {
        // MBAP 长度：6 字节 (UnitId + PDU)
        const ushort length = 6;

        byte[] frame = [
            (byte)(transactionId >> 8), // TID 高字节
            (byte)(transactionId & 0xFF),   // TID 低字节
            0,  // 协议ID高字节
            0,  // 协议ID低字节
            (length >> 8),  // 长度高字节
            (length & 0xFF),    // 长度低字节
            unitId, // 从站地址
            ModbusTcpConstants.FC_WRITE_SINGLE_REGISTER, // 功能码 FC06
            (byte)(address >> 8),   // 地址高字节
            (byte)(address & 0xFF), // 地址低字节
            (byte)(value >> 8), // 写入值高字节
            (byte)(value & 0xFF)    // 写入值低字节
        ];

        return frame;
    }

    /// <summary>
    /// 构建写多个保持寄存器请求报文（FC16）
    /// </summary>
    /// <param name="transactionId">事务ID，用于匹配请求与响应</param>
    /// <param name="unitId">从站地址（Unit ID）</param>
    /// <param name="startAddress">起始寄存器地址（0-based）</param>
    /// <param name="values">要写入的16位寄存器值数组</param>
    /// <returns>完整的 Modbus TCP 请求帧字节数组</returns>
    /// <exception cref="WriteErrorException"></exception>
    public static byte[] BuildWriteMultipleRegistersRequest(ushort transactionId, byte unitId, ushort startAddress, ushort[] values) {
        if (values.Length == 0) {
            throw new WriteErrorException("Values cannot be empty for WriteMultipleRegisters.");
        }

        ushort count = (ushort)values.Length;
        int byteCount = count * 2;

        // MBAP Length = UnitId(1) + PDU(1 + 2 + 2 + 1 + N) = 7 + N
        ushort length = (ushort)(7 + byteCount);

        byte[] frame = new byte[13 + byteCount];
        frame[0] = (byte)(transactionId >> 8);  // TID 高字节
        frame[1] = (byte)(transactionId & 0xFF);    // TID 低字节
        frame[2] = 0;   // 协议ID高字节
        frame[3] = 0;   // 协议ID低字节
        frame[4] = (byte)(length >> 8); // 长度高字节
        frame[5] = (byte)(length & 0xFF);   // 长度低字节
        frame[6] = unitId;  // 从站地址

        // 填充 PDU
        frame[7] = ModbusTcpConstants.FC_WRITE_MULTIPLE_REGISTERS; // 功能码 FC16
        frame[8] = (byte)(startAddress >> 8);   // 起始地址高字节
        frame[9] = (byte)(startAddress & 0xFF); // 起始地址低字节
        frame[10] = (byte)(count >> 8); // 写入数量高字节
        frame[11] = (byte)(count & 0xFF);   // 写入数量低字节
        frame[12] = (byte)byteCount;    // 数据字节数（= count * 2）

        int offset = 13;
        for (int i = 0; i < values.Length; i++) {
            ushort v = values[i];
            frame[offset++] = (byte)(v >> 8);
            frame[offset++] = (byte)(v & 0xFF);
        }

        return frame;
    }

    /// <summary>
    /// 构建写单个线圈请求报文（FC05）
    /// </summary>
    /// <param name="transactionId">事务ID，用于匹配请求与响应</param>
    /// <param name="unitId">从站地址（Unit ID）</param>
    /// <param name="address">目标线圈地址（0-based）</param>
    /// <param name="isOn">是否将线圈置为“开”状态（true = 0xFF00, false = 0x0000）</param>
    /// <returns>完整的 Modbus TCP 请求帧字节数组</returns>
    public static byte[] BuildWriteSingleCoilRequest(ushort transactionId, byte unitId, ushort address, bool isOn) {
        // MBAP Length = UnitId(1) + PDU(1+2+2) = 6
        const ushort length = 6;

        ushort coilValue = isOn ? (ushort)0xFF00 : (ushort)0x0000;

        byte[] frame = [
            (byte)(transactionId >> 8), // TID 高字节
            (byte)(transactionId & 0xFF),   // TID 低字节
            0,  // 协议ID高字节
            0,  // 协议ID低字节
            (length >> 8),  // 长度高字节
            (length & 0xFF),    // 长度低字节
            unitId, // 从站地址
            ModbusTcpConstants.FC_WRITE_SINGLE_COIL,   // 功能码 FC05
            (byte)(address >> 8),   // 地址高字节
            (byte)(address & 0xFF), // 地址低字节
            (byte)(coilValue >> 8), // 值高字节（FF 或 00）
            (byte)(coilValue & 0xFF)    // 值低字节（00）
        ];

        return frame;
    }

    /// <summary>
    /// 构建写多个线圈请求报文（FC15）
    /// </summary>
    /// <param name="transactionId">事务ID，用于匹配请求与响应</param>
    /// <param name="unitId">从站地址（Unit ID）</param>
    /// <param name="startAddress">起始线圈地址（0-based）</param>
    /// <param name="values">要写入的布尔值数组（true = 置位，false = 复位）</param>
    /// <returns>完整的 Modbus TCP 请求帧字节数组</returns>
    /// <exception cref="WriteErrorException"></exception>
    public static byte[] BuildWriteMultipleCoilsRequest(ushort transactionId, byte unitId, ushort startAddress, bool[] values) {
        if (values.Length == 0) {
            throw new WriteErrorException("Values cannot be empty for WriteMultipleCoils.");
        }

        ushort count = (ushort)values.Length;
        int byteCount = (values.Length + 7) / 8;

        // MBAP Length = UnitId(1) + PDU(1 + 2 + 2 + 1 + N) = 7 + N
        ushort length = (ushort)(7 + byteCount);

        byte[] frame = new byte[13 + byteCount];
        frame[0] = (byte)(transactionId >> 8);  // TID 高字节
        frame[1] = (byte)(transactionId & 0xFF);    // TID 低字节
        frame[2] = 0;   // 协议ID高字节
        frame[3] = 0;   // 协议ID低字节
        frame[4] = (byte)(length >> 8); // 长度高字节
        frame[5] = (byte)(length & 0xFF);   // 长度低字节
        frame[6] = unitId;  // 从站地址

        // 填充 PDU
        frame[7] = ModbusTcpConstants.FC_WRITE_MULTIPLE_COILS; // 功能码 FC15
        frame[8] = (byte)(startAddress >> 8);   // 起始地址高字节
        frame[9] = (byte)(startAddress & 0xFF); // 起始地址低字节
        frame[10] = (byte)(count >> 8); // 写入数量高字节
        frame[11] = (byte)(count & 0xFF);   // 写入数量低字节
        frame[12] = (byte)byteCount;    // 数据字节数（紧凑打包后的长度）

        int offset = 13;
        for (int i = 0; i < byteCount; i++) {
            byte b = 0;
            int baseBit = i * 8;
            int maxBit = Math.Min(8, values.Length - baseBit);

            for (int bit = 0; bit < maxBit; bit++) {
                if (values[baseBit + bit]) {
                    b |= (byte)(1 << bit);
                }
            }

            frame[offset + i] = b;
        }

        return frame;
    }
}

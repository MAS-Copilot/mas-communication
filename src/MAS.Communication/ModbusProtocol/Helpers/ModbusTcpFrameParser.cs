// =============================================================================
// Professional Automation Equipment Manufacturer.
// Website:       https://www.mas-automation.com/
//
// Copyright (c) MAS(厦门威光) Corporation. All rights reserved.
// =============================================================================

namespace MAS.Communication.ModbusProtocol;

/// <summary>
/// Modbus TCP 响应帧解析工具类
/// </summary>
internal static class ModbusTcpFrameParser {
    /// <summary>
    /// 解析读取寄存器（FC03/FC04）的响应报文，验证头部信息并提取寄存器值
    /// </summary>
    /// <param name="frame">接收到的完整字节帧</param>
    /// <param name="expectedTransactionId">期望的事务ID，用于匹配请求</param>
    /// <param name="expectedUnitId">期望的从站地址</param>
    /// <param name="expectedFunctionCode">期望的功能码</param>
    /// <param name="expectedCount">期望读取的寄存器数量</param>
    /// <returns>解析出的 16 位寄存器数组</returns>
    /// <exception cref="ReadErrorException"></exception>
    public static ushort[] ParseReadRegistersResponse(
        byte[] frame,
        ushort expectedTransactionId,
        byte expectedUnitId,
        byte expectedFunctionCode,
        ushort expectedCount) {

        if (frame.Length < 9) {
            throw new ReadErrorException($"Modbus response too short. Length={frame.Length}.");
        }

        ushort tid = (ushort)((frame[0] << 8) | frame[1]);
        if (tid != expectedTransactionId) {
            throw new ReadErrorException($"TransactionId mismatch. Expected={expectedTransactionId}, Actual={tid}.");
        }

        ushort pid = (ushort)((frame[2] << 8) | frame[3]);
        if (pid != ModbusTcpConstants.PROTOCOL_ID) {
            throw new ReadErrorException($"ProtocolId mismatch. Expected=0, Actual={pid}.");
        }

        ushort length = (ushort)((frame[4] << 8) | frame[5]);
        int expectedFrameLength = 6 + length;
        if (frame.Length != expectedFrameLength) {
            throw new ReadErrorException($"Frame length mismatch. MBAP says {expectedFrameLength}, Actual={frame.Length}.");
        }

        byte unitId = frame[6];
        if (unitId != expectedUnitId) {
            throw new ReadErrorException($"UnitId mismatch. Expected={expectedUnitId}, Actual={unitId}.");
        }

        byte fc = frame[7];
        if ((fc & ModbusTcpConstants.EXCEPTION_MASK) != 0) {
            byte exceptionCode = frame.Length > 8 ? frame[8] : (byte)0xFF;
            throw new ReadErrorException($"Modbus exception response. Function=0x{expectedFunctionCode:X2}, Code=0x{exceptionCode:X2}.");
        }

        if (fc != expectedFunctionCode) {
            throw new ReadErrorException($"FunctionCode mismatch. Expected=0x{expectedFunctionCode:X2}, Actual=0x{fc:X2}.");
        }

        byte byteCount = frame[8];
        int expectedByteCount = expectedCount * 2;
        if (byteCount != expectedByteCount) {
            throw new ReadErrorException($"ByteCount mismatch. Expected={expectedByteCount}, Actual={byteCount}.");
        }

        if (frame.Length < 9 + byteCount) {
            throw new ReadErrorException($"Payload too short. Need={9 + byteCount}, Actual={frame.Length}.");
        }

        ushort[] result = new ushort[expectedCount];
        int offset = 9;
        for (int i = 0; i < expectedCount; i++) {
            result[i] = (ushort)((frame[offset] << 8) | frame[offset + 1]);
            offset += 2;
        }

        return result;
    }

    /// <summary>
    /// 解析读取位变量（线圈、离散输入）的响应报文，验证帧头并提取布尔数组
    /// </summary>
    /// <param name="frame">接收到的完整字节帧</param>
    /// <param name="expectedTransactionId">期望的事务ID，用于匹配请求</param>
    /// <param name="expectedUnitId">期望的从站地址</param>
    /// <param name="expectedFunctionCode">期望的功能码</param>
    /// <param name="expectedCount">期望读取的位数量</param>
    /// <returns>解析出的布尔值数组（true = 1, false = 0）</returns>
    /// <exception cref="ReadErrorException"></exception>
    public static bool[] ParseReadBitsResponse(
        byte[] frame,
        ushort expectedTransactionId,
        byte expectedUnitId,
        byte expectedFunctionCode,
        ushort expectedCount) {
        if (frame.Length < 9) {
            throw new ReadErrorException($"Modbus response too short. Length={frame.Length}.");
        }

        ushort tid = (ushort)((frame[0] << 8) | frame[1]);
        if (tid != expectedTransactionId) {
            throw new ReadErrorException($"TransactionId mismatch. Expected={expectedTransactionId}, Actual={tid}.");
        }

        ushort pid = (ushort)((frame[2] << 8) | frame[3]);
        if (pid != ModbusTcpConstants.PROTOCOL_ID) {
            throw new ReadErrorException($"ProtocolId mismatch. Expected=0, Actual={pid}.");
        }

        ushort length = (ushort)((frame[4] << 8) | frame[5]);
        int expectedFrameLength = 6 + length;
        if (frame.Length != expectedFrameLength) {
            throw new ReadErrorException($"Frame length mismatch. MBAP says {expectedFrameLength}, Actual={frame.Length}.");
        }

        byte unitId = frame[6];
        if (unitId != expectedUnitId) {
            throw new ReadErrorException($"UnitId mismatch. Expected={expectedUnitId}, Actual={unitId}.");
        }

        byte fc = frame[7];
        if ((fc & ModbusTcpConstants.EXCEPTION_MASK) != 0) {
            byte exceptionCode = frame.Length > 8 ? frame[8] : (byte)0xFF;
            throw new ReadErrorException($"Modbus exception response. Function=0x{expectedFunctionCode:X2}, Code=0x{exceptionCode:X2}.");
        }

        if (fc != expectedFunctionCode) {
            throw new ReadErrorException($"FunctionCode mismatch. Expected=0x{expectedFunctionCode:X2}, Actual=0x{fc:X2}.");
        }

        byte byteCount = frame[8];
        int expectedByteCount = (expectedCount + 7) / 8;
        if (byteCount != expectedByteCount) {
            throw new ReadErrorException($"ByteCount mismatch. Expected={expectedByteCount}, Actual={byteCount}.");
        }

        if (frame.Length < 9 + byteCount) {
            throw new ReadErrorException($"Payload too short. Need={9 + byteCount}, Actual={frame.Length}.");
        }

        bool[] result = new bool[expectedCount];
        int dataOffset = 9;
        for (int i = 0; i < expectedCount; i++) {
            int byteIndex = i / 8;
            int bitIndex = i % 8;
            byte b = frame[dataOffset + byteIndex];
            result[i] = ((b >> bitIndex) & 0x01) == 1;
        }

        return result;
    }

    /// <summary>
    /// 验证写单个寄存器（FC06）的响应报文，确保操作成功并回显正确数据
    /// </summary>
    /// <param name="frame">接收到的完整响应字节帧</param>
    /// <param name="expectedTransactionId">期望的事务ID</param>
    /// <param name="expectedUnitId">期望的从站地址</param>
    /// <param name="expectedAddress">期望写入的寄存器地址</param>
    /// <param name="expectedValue">期望写入并被回显的寄存器值</param>
    /// <exception cref="WriteErrorException"></exception>
    public static void ValidateWriteSingleRegisterResponse(
        byte[] frame,
        ushort expectedTransactionId,
        byte expectedUnitId,
        ushort expectedAddress,
        ushort expectedValue) {
        ValidateMbap(frame, expectedTransactionId, expectedUnitId);

        if (frame.Length < 12) {
            throw new WriteErrorException($"Write single register response too short. Length={frame.Length}.");
        }

        byte fc = frame[7];
        if ((fc & ModbusTcpConstants.EXCEPTION_MASK) != 0) {
            byte exceptionCode = frame.Length > 8 ? frame[8] : (byte)0xFF;
            throw new WriteErrorException($"Modbus exception response. Function=0x{ModbusTcpConstants.FC_WRITE_SINGLE_REGISTER:X2}, Code=0x{exceptionCode:X2}.");
        }

        if (fc != ModbusTcpConstants.FC_WRITE_SINGLE_REGISTER) {
            throw new WriteErrorException($"FunctionCode mismatch. Expected=0x{ModbusTcpConstants.FC_WRITE_SINGLE_REGISTER:X2}, Actual=0x{fc:X2}.");
        }

        ushort addr = (ushort)((frame[8] << 8) | frame[9]);
        ushort val = (ushort)((frame[10] << 8) | frame[11]);

        if (addr != expectedAddress) {
            throw new WriteErrorException($"Address mismatch. Expected={expectedAddress}, Actual={addr}.");
        }

        if (val != expectedValue) {
            throw new WriteErrorException($"Value echo mismatch. Expected={expectedValue}, Actual={val}.");
        }
    }

    /// <summary>
    /// 验证写多个寄存器（FC16）的响应报文，确保批量写入操作被设备成功确认
    /// </summary>
    /// <param name="frame">接收到的完整响应字节帧</param>
    /// <param name="expectedTransactionId">期望的事务ID，用于匹配请求</param>
    /// <param name="expectedUnitId">期望的从站地址（Unit ID）</param>
    /// <param name="expectedStartAddress">期望写入的起始寄存器地址（0-based）</param>
    /// <param name="expectedCount">期望写入的寄存器数量</param>
    /// <exception cref="WriteErrorException"></exception>
    public static void ValidateWriteMultipleRegistersResponse(
        byte[] frame,
        ushort expectedTransactionId,
        byte expectedUnitId,
        ushort expectedStartAddress,
        ushort expectedCount) {
        ValidateMbap(frame, expectedTransactionId, expectedUnitId);

        if (frame.Length < 12) {
            throw new WriteErrorException($"Write multiple registers response too short. Length={frame.Length}.");
        }

        byte fc = frame[7];
        if ((fc & ModbusTcpConstants.EXCEPTION_MASK) != 0) {
            byte exceptionCode = frame.Length > 8 ? frame[8] : (byte)0xFF;
            throw new WriteErrorException($"Modbus exception response. Function=0x{ModbusTcpConstants.FC_WRITE_MULTIPLE_REGISTERS:X2}, Code=0x{exceptionCode:X2}.");
        }

        if (fc != ModbusTcpConstants.FC_WRITE_MULTIPLE_REGISTERS) {
            throw new WriteErrorException($"FunctionCode mismatch. Expected=0x{ModbusTcpConstants.FC_WRITE_MULTIPLE_REGISTERS:X2}, Actual=0x{fc:X2}.");
        }

        ushort addr = (ushort)((frame[8] << 8) | frame[9]);
        ushort count = (ushort)((frame[10] << 8) | frame[11]);

        if (addr != expectedStartAddress) {
            throw new WriteErrorException($"StartAddress mismatch. Expected={expectedStartAddress}, Actual={addr}.");
        }

        if (count != expectedCount) {
            throw new WriteErrorException($"WriteCount mismatch. Expected={expectedCount}, Actual={count}.");
        }
    }

    /// <summary>
    /// 验证写单个线圈（FC05）的响应报文
    /// </summary>
    /// <param name="frame">接收到的完整响应字节帧</param>
    /// <param name="expectedTransactionId">期望的事务ID，用于匹配请求</param>
    /// <param name="expectedUnitId">期望的从站地址（Unit ID）</param>
    /// <param name="expectedAddress">期望写入的线圈地址（0-based）</param>
    /// <param name="isExpectedOn">期望线圈被设置为“开”状态（true 表示 ON=0xFF00，false 表示 OFF=0x0000）</param>
    /// <exception cref="WriteErrorException"></exception>
    public static void ValidateWriteSingleCoilResponse(
        byte[] frame,
        ushort expectedTransactionId,
        byte expectedUnitId,
        ushort expectedAddress,
        bool isExpectedOn) {
        ValidateMbap(frame, expectedTransactionId, expectedUnitId);

        if (frame.Length < 12) {
            throw new WriteErrorException($"Write single coil response too short. Length={frame.Length}.");
        }

        byte fc = frame[7];
        if ((fc & ModbusTcpConstants.EXCEPTION_MASK) != 0) {
            byte exceptionCode = frame.Length > 8 ? frame[8] : (byte)0xFF;
            throw new WriteErrorException($"Modbus exception response. Function=0x{ModbusTcpConstants.FC_WRITE_SINGLE_COIL:X2}, Code=0x{exceptionCode:X2}.");
        }

        if (fc != ModbusTcpConstants.FC_WRITE_SINGLE_COIL) {
            throw new WriteErrorException($"FunctionCode mismatch. Expected=0x{ModbusTcpConstants.FC_WRITE_SINGLE_COIL:X2}, Actual=0x{fc:X2}.");
        }

        ushort addr = (ushort)((frame[8] << 8) | frame[9]);
        ushort val = (ushort)((frame[10] << 8) | frame[11]);

        ushort expectedVal = isExpectedOn ? (ushort)0xFF00 : (ushort)0x0000;

        if (addr != expectedAddress) {
            throw new WriteErrorException($"Address mismatch. Expected={expectedAddress}, Actual={addr}.");
        }

        if (val != expectedVal) {
            throw new WriteErrorException($"Value echo mismatch. Expected=0x{expectedVal:X4}, Actual=0x{val:X4}.");
        }
    }

    /// <summary>
    /// 验证写多个线圈（FC15）的响应报文
    /// </summary>
    /// <param name="frame">接收到的完整响应字节帧</param>
    /// <param name="expectedTransactionId">期望的事务ID，用于匹配请求</param>
    /// <param name="expectedUnitId">期望的从站地址（Unit ID）</param>
    /// <param name="expectedStartAddress">期望写入的起始线圈地址（0-based）</param>
    /// <param name="expectedCount">期望写入的线圈数量</param>
    /// <exception cref="WriteErrorException"></exception>
    public static void ValidateWriteMultipleCoilsResponse(
        byte[] frame,
        ushort expectedTransactionId,
        byte expectedUnitId,
        ushort expectedStartAddress,
        ushort expectedCount) {
        ValidateMbap(frame, expectedTransactionId, expectedUnitId);

        if (frame.Length < 12) {
            throw new WriteErrorException($"Write multiple coils response too short. Length={frame.Length}.");
        }

        byte fc = frame[7];
        if ((fc & ModbusTcpConstants.EXCEPTION_MASK) != 0) {
            byte exceptionCode = frame.Length > 8 ? frame[8] : (byte)0xFF;
            throw new WriteErrorException($"Modbus exception response. Function=0x{ModbusTcpConstants.FC_WRITE_MULTIPLE_COILS:X2}, Code=0x{exceptionCode:X2}.");
        }

        if (fc != ModbusTcpConstants.FC_WRITE_MULTIPLE_COILS) {
            throw new WriteErrorException($"FunctionCode mismatch. Expected=0x{ModbusTcpConstants.FC_WRITE_MULTIPLE_COILS:X2}, Actual=0x{fc:X2}.");
        }

        ushort addr = (ushort)((frame[8] << 8) | frame[9]);
        ushort count = (ushort)((frame[10] << 8) | frame[11]);

        if (addr != expectedStartAddress) {
            throw new WriteErrorException($"StartAddress mismatch. Expected={expectedStartAddress}, Actual={addr}.");
        }

        if (count != expectedCount) {
            throw new WriteErrorException($"WriteCount mismatch. Expected={expectedCount}, Actual={count}.");
        }
    }

    #region 私有方法

    private static void ValidateMbap(byte[] frame, ushort expectedTransactionId, byte expectedUnitId) {
        if (frame.Length < 8) {
            throw new WriteErrorException($"Modbus response too short. Length={frame.Length}.");
        }

        ushort tid = (ushort)((frame[0] << 8) | frame[1]);
        if (tid != expectedTransactionId) {
            throw new WriteErrorException($"TransactionId mismatch. Expected={expectedTransactionId}, Actual={tid}.");
        }

        ushort pid = (ushort)((frame[2] << 8) | frame[3]);
        if (pid != ModbusTcpConstants.PROTOCOL_ID) {
            throw new WriteErrorException($"ProtocolId mismatch. Expected=0, Actual={pid}.");
        }

        ushort length = (ushort)((frame[4] << 8) | frame[5]);
        int expectedFrameLength = 6 + length;
        if (frame.Length != expectedFrameLength) {
            throw new WriteErrorException($"Frame length mismatch. MBAP says {expectedFrameLength}, Actual={frame.Length}.");
        }

        byte unitId = frame[6];
        if (unitId != expectedUnitId) {
            throw new WriteErrorException($"UnitId mismatch. Expected={expectedUnitId}, Actual={unitId}.");
        }
    }

    #endregion
}

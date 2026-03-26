// =============================================================================
// Professional Automation Equipment Manufacturer.
// Website:       https://www.mas-automation.com/
//
// Copyright (c) MAS(厦门威光) Corporation. All rights reserved.
// =============================================================================

namespace MAS.Communication.ModbusProtocol;

/// <summary>
/// Modbus 寄存器与字节数组转换
/// </summary>
/// <remarks>
/// 适配：Modbus 寄存器使用 Big-endian 存储，但 StructBinaryHelper 按 Little-endian 处理
/// </remarks>
internal static class ModbusRegisterBinaryHelper {
    /// <summary>
    /// 将 Modbus 寄存器数组转换为小端字节数组（按需截断或填充）
    /// </summary>
    /// <param name="registers">寄存器数组（每个寄存器 16 位，大端存储）</param>
    /// <param name="requiredBytes">期望输出的字节长度</param>
    /// <returns>小端排列的字节数组（低位在前）</returns>
    public static byte[] RegistersToLittleEndianBytes(ushort[] registers, int requiredBytes) {
        int totalBytes = registers.Length * 2;
        if (requiredBytes < 0 || requiredBytes > totalBytes) {
            throw new ArgumentOutOfRangeException(nameof(requiredBytes));
        }

        byte[] bytes = new byte[requiredBytes];
        int offset = 0;

        for (int i = 0; i < registers.Length && offset < requiredBytes; i++) {
            ushort r = registers[i];

            byte lo = (byte)(r & 0xFF);
            byte hi = (byte)(r >> 8);

            if (offset < requiredBytes) {
                bytes[offset++] = lo;
            }

            if (offset < requiredBytes) {
                bytes[offset++] = hi;
            }
        }

        return bytes;
    }

    /// <summary>
    /// 将 Modbus 寄存器数组转换为字节数组，支持自定义字节序和字词序
    /// </summary>
    /// <param name="registers">原始寄存器数组</param>
    /// <param name="requiredBytes">目标字节数（截断或补全）</param>
    /// <param name="byteOrder">寄存器内字节排列顺序</param>
    /// <param name="wordOrder">多寄存器组合时的顺序</param>
    /// <returns>按指定序规则排列的字节数组</returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static byte[] RegistersToLittleEndianBytes(
        ushort[] registers,
        int requiredBytes,
        ModbusByteOrder byteOrder,
        ModbusWordOrder wordOrder) {

        int totalBytes = registers.Length * 2;
        if (requiredBytes < 0 || requiredBytes > totalBytes) {
            throw new ArgumentOutOfRangeException(nameof(requiredBytes));
        }

        ushort[] ordered = ApplyWordOrder(registers, wordOrder);

        byte[] bytes = new byte[requiredBytes];
        int offset = 0;

        for (int i = 0; i < ordered.Length && offset < requiredBytes; i++) {
            ushort r = ordered[i];

            byte hi = (byte)(r >> 8);
            byte lo = (byte)(r & 0xFF);

            byte a, b;
            if (byteOrder == ModbusByteOrder.BigEndian) {
                a = hi; b = lo;
            } else {
                a = lo; b = hi;
            }

            if (offset < requiredBytes) {
                bytes[offset++] = b;
            }

            if (offset < requiredBytes) {
                bytes[offset++] = a;
            }
        }

        return bytes;
    }

    /// <summary>
    /// 将小端字节数组还原为 Modbus 寄存器数组（自动补零填充）
    /// </summary>
    /// <param name="bytes">原始字节数组（按小端布局）</param>
    /// <returns>对应的寄存器数组，不足部分补零</returns>
    public static ushort[] LittleEndianBytesToRegisters(byte[] bytes) {
        int registerCount = (bytes.Length + 1) / 2;
        ushort[] registers = new ushort[registerCount];

        int offset = 0;
        for (int i = 0; i < registerCount; i++) {
            byte lo = offset < bytes.Length ? bytes[offset++] : (byte)0;
            byte hi = offset < bytes.Length ? bytes[offset++] : (byte)0;

            registers[i] = (ushort)(lo | (hi << 8));
        }

        return registers;
    }

    /// <summary>
    /// 将字节数组还原为 Modbus 寄存器数组，支持自定义字节序和字词序
    /// </summary>
    /// <param name="bytes">原始字节数组</param>
    /// <param name="byteOrder">寄存器内字节排列顺序</param>
    /// <param name="wordOrder">多寄存器组合时的顺序（如高低字交换）</param>
    /// <returns>按指定序规则排列的寄存器数组，不足部分补零</returns>
    public static ushort[] LittleEndianBytesToRegisters(
        byte[] bytes,
        ModbusByteOrder byteOrder,
        ModbusWordOrder wordOrder) {

        int registerCount = (bytes.Length + 1) / 2;
        ushort[] registers = new ushort[registerCount];

        int offset = 0;
        for (int i = 0; i < registerCount; i++) {
            byte b = offset < bytes.Length ? bytes[offset++] : (byte)0;
            byte a = offset < bytes.Length ? bytes[offset++] : (byte)0;

            byte hi = a;
            byte lo = b;

            ushort r = byteOrder == ModbusByteOrder.BigEndian
                ? (ushort)((hi << 8) | lo)
                : (ushort)((lo << 8) | hi);

            registers[i] = r;
        }

        return ApplyWordOrder(registers, wordOrder);
    }

    #region 私有方法

    private static ushort[] ApplyWordOrder(ushort[] registers, ModbusWordOrder wordOrder) {
        if (wordOrder == ModbusWordOrder.Normal || registers.Length < 2) {
            return registers;
        }

        ushort[] copy = new ushort[registers.Length];
        Array.Copy(registers, copy, registers.Length);

        for (int i = 0; i + 1 < copy.Length; i += 2) {
            (copy[i], copy[i + 1]) = (copy[i + 1], copy[i]);
        }

        return copy;
    }

    #endregion
}

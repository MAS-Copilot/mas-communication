// =============================================================================
// MAS.Communication
// https://www.mas-automation.com/
//
// Copyright 2026 MAS (厦门威光) Corporation
//
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.
// =============================================================================

using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace MAS.Communication;

/// <summary>
/// 结构体与字节数组的通用编解码
/// </summary>
public static class StructBinaryHelper {
    private static readonly Dictionary<Type, Func<byte[], int, object>> TypeConversionMap = new() {
        { typeof(double), (b, o) => BitConverter.ToDouble(b, o) },
        { typeof(float), (b, o) => BitConverter.ToSingle(b, o) },
        { typeof(short), (b, o) => BitConverter.ToInt16(b, o) },
        { typeof(int), (b, o) => BitConverter.ToInt32(b, o) },
        { typeof(long), (b, o) => BitConverter.ToInt64(b, o) },
        { typeof(ushort), (b, o) => BitConverter.ToUInt16(b, o) },
        { typeof(uint), (b, o) => BitConverter.ToUInt32(b, o) },
        { typeof(ulong), (b, o) => BitConverter.ToUInt64(b, o) },
        { typeof(byte), (b, o) => b[o] }
    };

    /// <summary>
    /// 获取结构体总大小
    /// </summary>
    /// <param name="structType">结构体类型</param>
    /// <returns>所需字节数</returns>
    public static int GetStructSize(Type structType) {
        double numBytes = 0.0;
        var fields = structType.GetFields();

        foreach (var field in fields) {
            if (field.FieldType == typeof(bool)) {
                numBytes += 0.125;
            }
        }

        numBytes = Math.Ceiling(numBytes / 2) * 2;

        foreach (var field in fields) {
            if (field.FieldType == typeof(bool)) {
                continue;
            }

            numBytes = field.FieldType.Name switch {
                "Byte" => Math.Ceiling(numBytes) + 1,
                "Int16" or "UInt16" => Math.Ceiling(numBytes) + 2,
                "Int32" or "UInt32" or "Single" => Math.Ceiling(numBytes) + 4,
                "Int64" or "UInt64" => Math.Ceiling(numBytes) + 8,
                "Double" => Math.Ceiling(numBytes) + 8,
                "String" => Math.Ceiling(numBytes) + GetFixedStringLength(structType, field),
                _ => Math.Ceiling(numBytes) + GetStructSize(field.FieldType)
            };
        }

        return (int)Math.Ceiling(numBytes);
    }

    /// <summary>
    /// 将字节数组转换为结构体实例
    /// </summary>
    /// <param name="bytes">源字节数组</param>
    /// <param name="structType">目标结构体类型</param>
    /// <returns>结构体实例</returns>
    public static object BytesToStruct(byte[] bytes, Type structType) {
        object structValue = Activator.CreateInstance(structType)!;
        var fields = structType.GetFields();
        double bitPos = 0.0;

        foreach (var field in fields) {
            if (field.FieldType == typeof(bool)) {
                bitPos = ReadBool(field, structValue, bytes, bitPos);
            }
        }

        if (bitPos / 2 != 0) {
            bitPos = Math.Ceiling(bitPos / 2) * 2;
        }

        foreach (var field in fields) {
            if (field.FieldType == typeof(bool)) {
                continue;
            }

            bitPos = ReadField(field, structValue, bytes, bitPos);
        }

        return structValue;
    }

    /// <summary>
    /// 将字节数组转换为指定结构体类型的实例
    /// </summary>
    /// <typeparam name="T">结构体类型</typeparam>
    /// <param name="bytes">源字节数组</param>
    /// <returns>结构体实例</returns>
    public static T BytesToStruct<T>(byte[] bytes) where T : struct {
        return (T)BytesToStruct(bytes, typeof(T));
    }

    /// <summary>
    /// 将结构体实例序列化为字节数组
    /// </summary>
    /// <param name="structValue">结构体实例</param>
    /// <returns>字节数组</returns>
    public static byte[] StructToBytes(object structValue) {
        var structType = structValue.GetType();
        var fields = structType.GetFields();
        int numBytes = GetStructSize(structType);
        byte[] bytes = new byte[numBytes];
        double bitPos = 0.0;

        foreach (var field in fields) {
            if (field.FieldType == typeof(bool)) {
                WriteBool(field, structValue, bytes, ref bitPos);
            }
        }

        if (bitPos / 2 != 0) {
            bitPos = Math.Ceiling(bitPos / 2) * 2;
        }

        foreach (var field in fields) {
            if (field.FieldType == typeof(bool)) {
                continue;
            }

            WriteField(field, structValue, bytes, ref bitPos);
        }

        return bytes;
    }

    /// <summary>
    /// 将结构体复制到字节数组的指定区域（支持偏移和截断）
    /// </summary>
    /// <param name="structValue">结构体实例，null 时清空目标区域</param>
    /// <param name="bytes">目标字节数组</param>
    /// <param name="offset">起始偏移</param>
    /// <param name="size">复制长度</param>
    public static void CopyStructToBytes(object? structValue, byte[] bytes, int offset, int size) {
        if (structValue == null) {
            Array.Clear(bytes, offset, size);
            return;
        }

        byte[] fieldBytes = new byte[size];
        GCHandle handle = GCHandle.Alloc(fieldBytes, GCHandleType.Pinned);
        try {
            Marshal.StructureToPtr(structValue, handle.AddrOfPinnedObject(), false);
        } finally {
            handle.Free();
        }

        Array.Copy(fieldBytes, 0, bytes, offset, size);
    }

    /// <summary>
    /// 获取基础类型的字节长度
    /// </summary>
    /// <param name="type">类型</param>
    /// <returns>字节大小</returns>
    /// <exception cref="NotSupportedException"></exception>
    public static int GetTypeByteLength(Type type) {
        if (type == typeof(short) || type == typeof(ushort)) {
            return 2;
        }

        if (type == typeof(int) || type == typeof(uint) || type == typeof(float)) {
            return 4;
        }

        if (type == typeof(long) || type == typeof(ulong) || type == typeof(double)) {
            return 8;
        }

        if (type == typeof(byte)) {
            return 1;
        }

        throw new NotSupportedException($"Type {type.Name} is not supported.");
    }

    /// <summary>
    /// 从字节数组中提取指定类型的值
    /// </summary>
    /// <param name="bytes">源字节数组</param>
    /// <param name="targetType">目标类型</param>
    /// <param name="offset">起始偏移</param>
    /// <returns>对应类型的值</returns>
    /// <exception cref="NotSupportedException"></exception>
    public static object GetValueFromBytes(byte[] bytes, Type targetType, int offset = 0) {
        if (TypeConversionMap.TryGetValue(targetType, out var convert)) {
            return convert(bytes, offset);
        }

        throw new NotSupportedException($"不支持的类型: {targetType.Name}");
    }

    /// <summary>
    /// 将值转换为字节数组
    /// </summary>
    /// <param name="value">原始值</param>
    /// <returns>对应的字节数组</returns>
    /// <exception cref="NotSupportedException"></exception>
    public static byte[] GetBytes(object value) {
        return value switch {
            short v => BitConverter.GetBytes(v),
            ushort v => BitConverter.GetBytes(v),
            int v => BitConverter.GetBytes(v),
            uint v => BitConverter.GetBytes(v),
            long v => BitConverter.GetBytes(v),
            ulong v => BitConverter.GetBytes(v),
            float v => BitConverter.GetBytes(v),
            double v => BitConverter.GetBytes(v),
            byte v => [v],
            _ => throw new NotSupportedException($"Type {value.GetType().Name} is not supported.")
        };
    }

    #region private helpers

    private static double ReadField(FieldInfo field, object structValue, byte[] bytes, double bitPos) {
        return field.FieldType.Name switch {
            "Byte" => ReadByte(field, structValue, bytes, bitPos),
            "Int16" => ReadInt16(field, structValue, bytes, bitPos),
            "UInt16" => ReadUInt16(field, structValue, bytes, bitPos),
            "Int32" => ReadInt32(field, structValue, bytes, bitPos),
            "UInt32" => ReadUInt32(field, structValue, bytes, bitPos),
            "Int64" => ReadInt64(field, structValue, bytes, bitPos),
            "UInt64" => ReadUInt64(field, structValue, bytes, bitPos),
            "Single" => ReadFloat(field, structValue, bytes, bitPos),
            "Double" => ReadDouble(field, structValue, bytes, bitPos),
            "String" => ReadString(field, structValue, bytes, bitPos),
            _ => ReadNested(field, structValue, bytes, bitPos)
        };
    }

    private static double ReadBool(FieldInfo field, object structValue, byte[] bytes, double bitPos) {
        int currentBytePos = (int)bitPos;
        int bitOffset = (int)((bitPos - currentBytePos) * 8);
        bool hasValue = (bytes[currentBytePos] & (byte)(1 << bitOffset)) != 0;
        field.SetValue(structValue, hasValue);
        return bitPos + 0.125;
    }

    private static double ReadByte(FieldInfo field, object structValue, byte[] bytes, double bitPos) {
        int bytePos = (int)Math.Ceiling(bitPos);
        field.SetValue(structValue, bytes[bytePos]);
        return bitPos + 1;
    }

    private static double ReadInt16(FieldInfo field, object structValue, byte[] bytes, double bitPos) {
        int bytePos = (int)Math.Ceiling(bitPos);
        short v = BitConverter.ToInt16(bytes, bytePos);
        field.SetValue(structValue, Convert.ChangeType(v, field.FieldType));
        return bitPos + 2;
    }

    private static double ReadUInt16(FieldInfo field, object structValue, byte[] bytes, double bitPos) {
        int bytePos = (int)Math.Ceiling(bitPos);
        ushort v = BitConverter.ToUInt16(bytes, bytePos);
        field.SetValue(structValue, v);
        return bitPos + 2;
    }

    private static double ReadInt32(FieldInfo field, object structValue, byte[] bytes, double bitPos) {
        int bytePos = (int)Math.Ceiling(bitPos);
        int v = BitConverter.ToInt32(bytes, bytePos);
        field.SetValue(structValue, Convert.ChangeType(v, field.FieldType));
        return bitPos + 4;
    }

    private static double ReadUInt32(FieldInfo field, object structValue, byte[] bytes, double bitPos) {
        int bytePos = (int)Math.Ceiling(bitPos);
        uint v = BitConverter.ToUInt32(bytes, bytePos);
        field.SetValue(structValue, v);
        return bitPos + 4;
    }

    private static double ReadInt64(FieldInfo field, object structValue, byte[] bytes, double bitPos) {
        int bytePos = (int)Math.Ceiling(bitPos);
        long v = BitConverter.ToInt64(bytes, bytePos);
        field.SetValue(structValue, v);
        return bitPos + 8;
    }

    private static double ReadUInt64(FieldInfo field, object structValue, byte[] bytes, double bitPos) {
        int bytePos = (int)Math.Ceiling(bitPos);
        ulong v = BitConverter.ToUInt64(bytes, bytePos);
        field.SetValue(structValue, v);
        return bitPos + 8;
    }

    private static double ReadFloat(FieldInfo field, object structValue, byte[] bytes, double bitPos) {
        int bytePos = (int)Math.Ceiling(bitPos);
        float v = BitConverter.ToSingle(bytes, bytePos);
        field.SetValue(structValue, v);
        return bitPos + 4;
    }

    private static double ReadDouble(FieldInfo field, object structValue, byte[] bytes, double bitPos) {
        int bytePos = (int)Math.Ceiling(bitPos);
        double v = BitConverter.ToDouble(bytes, bytePos);
        field.SetValue(structValue, v);
        return bitPos + 8;
    }

    private static double ReadString(FieldInfo field, object structValue, byte[] bytes, double bitPos) {
        var attr = field.GetCustomAttribute<FixedStringAttribute>()
            ?? throw new InvalidOperationException($"字符串字段 {field.Name} 缺少 FixedStringAttribute 特性");

        int bytePos = (int)Math.Ceiling(bitPos);
        Encoding encoding = Encoding.GetEncoding(attr.EncodingName);
        string s = encoding.GetString(bytes, bytePos, attr.Length);
        if (attr.TrimEndPadding) {
            s = s.TrimEnd(attr.PaddingChar, '\0');
        }

        field.SetValue(structValue, s);
        return bitPos + attr.Length;
    }

    private static double ReadNested(FieldInfo field, object structValue, byte[] bytes, double bitPos) {
        int bytePos = (int)Math.Ceiling(bitPos);
        int size = GetStructSize(field.FieldType);
        byte[] nestedBytes = new byte[size];
        Array.Copy(bytes, bytePos, nestedBytes, 0, size);
        object nested = BytesToStruct(nestedBytes, field.FieldType);
        field.SetValue(structValue, nested);
        return bitPos + size;
    }

    private static void WriteField(FieldInfo field, object structValue, byte[] bytes, ref double bitPos) {
        switch (field.FieldType.Name) {
            case "Byte":
                WriteByte(field, structValue, bytes, ref bitPos);
                break;
            case "Int16":
                WriteInt16(field, structValue, bytes, ref bitPos);
                break;
            case "UInt16":
                WriteUInt16(field, structValue, bytes, ref bitPos);
                break;
            case "Int32":
                WriteInt32(field, structValue, bytes, ref bitPos);
                break;
            case "UInt32":
                WriteUInt32(field, structValue, bytes, ref bitPos);
                break;
            case "Int64":
                WriteInt64(field, structValue, bytes, ref bitPos);
                break;
            case "UInt64":
                WriteUInt64(field, structValue, bytes, ref bitPos);
                break;
            case "Single":
                WriteFloat(field, structValue, bytes, ref bitPos);
                break;
            case "Double":
                WriteDouble(field, structValue, bytes, ref bitPos);
                break;
            case "String":
                WriteString(field, structValue, bytes, ref bitPos);
                break;
            default:
                WriteNested(field, structValue, bytes, ref bitPos);
                break;
        }
    }

    private static void WriteBool(FieldInfo field, object structValue, byte[] bytes, ref double bitPos) {
        int currentBytePos = (int)bitPos;
        int bitOffset = (int)((bitPos - currentBytePos) * 8);
        bool isOn = (bool)field.GetValue(structValue)!;

        if (isOn) {
            bytes[currentBytePos] |= (byte)(1 << bitOffset);
        } else {
            bytes[currentBytePos] &= (byte)~(1 << bitOffset);
        }

        bitPos += 0.125;
    }

    private static void WriteByte(FieldInfo field, object structValue, byte[] bytes, ref double bitPos) {
        int bytePos = (int)Math.Ceiling(bitPos);
        bytes[bytePos] = (byte)field.GetValue(structValue)!;
        bitPos += 1;
    }

    private static void WriteInt16(FieldInfo field, object structValue, byte[] bytes, ref double bitPos) {
        int bytePos = (int)Math.Ceiling(bitPos);
        short v = Convert.ToInt16(field.GetValue(structValue));
        byte[] bs = BitConverter.GetBytes(v);
        Array.Copy(bs, 0, bytes, bytePos, 2);
        bitPos += 2;
    }

    private static void WriteUInt16(FieldInfo field, object structValue, byte[] bytes, ref double bitPos) {
        int bytePos = (int)Math.Ceiling(bitPos);
        ushort v = Convert.ToUInt16(field.GetValue(structValue));
        byte[] bs = BitConverter.GetBytes(v);
        Array.Copy(bs, 0, bytes, bytePos, 2);
        bitPos += 2;
    }

    private static void WriteInt32(FieldInfo field, object structValue, byte[] bytes, ref double bitPos) {
        int bytePos = (int)Math.Ceiling(bitPos);
        int v = Convert.ToInt32(field.GetValue(structValue));
        byte[] bs = BitConverter.GetBytes(v);
        Array.Copy(bs, 0, bytes, bytePos, 4);
        bitPos += 4;
    }

    private static void WriteUInt32(FieldInfo field, object structValue, byte[] bytes, ref double bitPos) {
        int bytePos = (int)Math.Ceiling(bitPos);
        uint v = Convert.ToUInt32(field.GetValue(structValue));
        byte[] bs = BitConverter.GetBytes(v);
        Array.Copy(bs, 0, bytes, bytePos, 4);
        bitPos += 4;
    }

    private static void WriteInt64(FieldInfo field, object structValue, byte[] bytes, ref double bitPos) {
        int bytePos = (int)Math.Ceiling(bitPos);
        long v = Convert.ToInt64(field.GetValue(structValue));
        byte[] bs = BitConverter.GetBytes(v);
        Array.Copy(bs, 0, bytes, bytePos, 8);
        bitPos += 8;
    }

    private static void WriteUInt64(FieldInfo field, object structValue, byte[] bytes, ref double bitPos) {
        int bytePos = (int)Math.Ceiling(bitPos);
        ulong v = Convert.ToUInt64(field.GetValue(structValue));
        byte[] bs = BitConverter.GetBytes(v);
        Array.Copy(bs, 0, bytes, bytePos, 8);
        bitPos += 8;
    }

    private static void WriteFloat(FieldInfo field, object structValue, byte[] bytes, ref double bitPos) {
        int bytePos = (int)Math.Ceiling(bitPos);
        float v = Convert.ToSingle(field.GetValue(structValue));
        byte[] bs = BitConverter.GetBytes(v);
        Array.Copy(bs, 0, bytes, bytePos, 4);
        bitPos += 4;
    }

    private static void WriteDouble(FieldInfo field, object structValue, byte[] bytes, ref double bitPos) {
        int bytePos = (int)Math.Ceiling(bitPos);
        double v = Convert.ToDouble(field.GetValue(structValue));
        byte[] bs = BitConverter.GetBytes(v);
        Array.Copy(bs, 0, bytes, bytePos, 8);
        bitPos += 8;
    }

    private static void WriteString(FieldInfo field, object structValue, byte[] bytes, ref double bitPos) {
        var attr = field.GetCustomAttribute<FixedStringAttribute>()
            ?? throw new InvalidOperationException($"字符串字段 {field.Name} 缺少 FixedStringAttribute 特性");

        string s = field.GetValue(structValue) as string ?? string.Empty;
        if (s.Length > attr.Length) {
#if !NETFRAMEWORK
            s = s[..attr.Length];
#else
            s = s.Substring(0, attr.Length);
#endif
        }

        s = s.PadRight(attr.Length, attr.PaddingChar);
        Encoding encoding = Encoding.GetEncoding(attr.EncodingName);
        byte[] bs = encoding.GetBytes(s);

        int bytePos = (int)Math.Ceiling(bitPos);
        Array.Copy(bs, 0, bytes, bytePos, bs.Length);
        bitPos += bs.Length;
    }

    private static void WriteNested(FieldInfo field, object structValue, byte[] bytes, ref double bitPos) {
        int bytePos = (int)Math.Ceiling(bitPos);
        object nested = field.GetValue(structValue)!;
        byte[] nestedBytes = StructToBytes(nested);
        Array.Copy(nestedBytes, 0, bytes, bytePos, nestedBytes.Length);
        bitPos += nestedBytes.Length;
    }

    private static int GetFixedStringLength(Type structType, FieldInfo field) {
        var attr = field.GetCustomAttribute<FixedStringAttribute>()
            ?? throw new InvalidOperationException($"{structType.FullName} 中的字符串字段 {field.Name} 缺少 FixedStringAttribute 特性");
        return attr.Length;
    }

#endregion
}


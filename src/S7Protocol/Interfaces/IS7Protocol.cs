// =============================================================================
// MAS.Communication
// https://www.mas-automation.com/
//
// Copyright 2026 MAS (厦门威光) Corporation
//
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.
// =============================================================================

using S7.Net;
using S7.Net.Types;

namespace MAS.Communication.S7Protocol;

/// <summary>
/// 与西门子 S7 设备进行通信的接口
/// </summary>
public interface IS7Protocol : IProtocol {
    /// <summary>
    /// 异步从PLC读取字数据
    /// </summary>       
    /// <remarks>
    /// 这个方法适用于当系统需要直接处理或分析来自特定PLC内存区域的字节流，或者处理未经封装的简单数据类型时使用
    /// </remarks>
    /// <param name="dataType">数据类型</param>
    /// <param name="db">数据块编号</param>
    /// <param name="startByteAdr">起始字节地址</param>
    /// <param name="count">读取的字节数</param>
    /// <param name="cts">取消令牌</param>
    /// <returns>异步操作任务结果，读取到的字节数组，由调用方处理可能发生的异常</returns>
    /// <exception cref="OperationCanceledException">取消此操作时抛出此异常</exception>
    /// <exception cref="ReadErrorException">读取异常</exception>
    Task<byte[]> ReadBytesAsync(DataType dataType, int db, int startByteAdr, int count, CancellationToken cts = default);

    /// <summary>
    /// 异步写入字数据到PLC
    /// </summary>
    /// <remarks>
    /// 这个方法常用于低级别的数据写入操作，将字节数据直接写入PLC的指定数据块，设置PLC的配置参数或更新固件等
    /// </remarks>
    /// <param name="dataType">数据类型</param>
    /// <param name="db">数据块编号</param>
    /// <param name="startByteAdr">起始字节地址</param>
    /// <param name="value">要写入的字节数组</param>
    /// <param name="cts">取消令牌</param>
    /// <returns>异步操作任务结果，由调用方处理可能发生的异常</returns>
    /// <exception cref="OperationCanceledException">取消此操作时抛出此异常</exception>
    /// <exception cref="WriteErrorException">写入异常</exception>
    Task WriteBytesAsync(DataType dataType, int db, int startByteAdr, byte[] value, CancellationToken cts = default);

    /// <summary>
    /// 异步读取解码
    /// </summary>
    /// <remarks>
    /// 这个方法适用于需要根据特定的数据类型进行数据交换的场合，当需要读取特定类型的数据（整数、实数等）到PLC的数据块时，可以自动处理数据类型的转换
    /// </remarks>
    /// <param name="dataType">数据类型</param>
    /// <param name="db">数据块编号</param>
    /// <param name="startByteAdr">起始字节地址</param>
    /// <param name="varType">要转换字节的数据类型</param>
    /// <param name="varCount">要读取的变量数量</param>
    /// <param name="cts">取消令牌</param>
    /// <returns>异步操作任务结果，读取的数据对象，由调用方处理可能发生的异常</returns>
    /// <exception cref="OperationCanceledException">取消此操作时抛出此异常</exception>
    /// <exception cref="ReadErrorException">读取异常</exception>
    Task<object?> ReadAsync(DataType dataType, int db, int startByteAdr, VarType varType, int varCount, CancellationToken cts = default);

    /// <summary>
    /// 异步写入解码
    /// </summary>
    /// <remarks>
    /// 这个方法适用于需要根据特定的数据类型进行数据交换的场合，当需要写入特定类型的数据（整数、实数等）到PLC的数据块时，可以自动处理数据类型的转换
    /// </remarks>
    /// <param name="dataType">数据类型</param>
    /// <param name="db">数据块编号</param>
    /// <param name="startByteAdr">起始字节地址</param>
    /// <param name="value">要写入的数据对象，它可以是单个值或数组</param>
    /// <param name="cts">取消令牌</param>
    /// <returns>异步操作任务结果，由调用方处理可能发生的异常</returns>
    /// <exception cref="OperationCanceledException">取消此操作时抛出此异常</exception>
    /// <exception cref="WriteErrorException">读取异常</exception>
    Task WriteAsync(DataType dataType, int db, int startByteAdr, object value, CancellationToken cts = default);

    /// <summary>
    /// 异步读取PLC中的结构体数据
    /// </summary>
    /// <remarks>
    /// 这个方法适用于需要将PLC中的数据块映射到C#中的结构体
    /// </remarks>
    /// <typeparam name="T">结构体类型</typeparam>
    /// <param name="db">数据块编号</param>
    /// <param name="startByteAdr">起始字节地址，默认为0</param>
    /// <param name="cts">取消令牌</param>
    /// <returns>异步操作任务结果，读取的结构体数据，由调用方处理可能发生的异常</returns>
    /// <exception cref="OperationCanceledException">取消此操作时抛出此异常</exception>
    /// <exception cref="ReadErrorException">读取异常</exception>
    Task<T?> ReadStructAsync<T>(int db, int startByteAdr = 0, CancellationToken cts = default) where T : struct;

    /// <summary>
    /// 异步读取PLC中的结构体数据
    /// </summary>
    /// <remarks>
    /// 这个方法适用于需要将PLC中的数据块映射到C#中的结构体
    /// </remarks>
    /// <param name="structType">结构体类型</param>
    /// <param name="db">数据块编号</param>
    /// <param name="startByteAdr">起始字节地址，默认为0</param>
    /// <param name="cts">取消令牌</param>
    /// <returns>异步操作任务结果，读取的结构体数据，由调用方处理可能发生的异常</returns>
    /// <exception cref="OperationCanceledException">取消此操作时抛出此异常</exception>
    /// <exception cref="ReadErrorException">读取异常</exception>
    Task<object?> ReadStructAsync(Type structType, int db, int startByteAdr = 0, CancellationToken cts = default);

    /// <summary>
    /// 异步向PLC写入结构体数据
    /// </summary>
    /// <remarks>
    /// 这个方法适用于需要将C#中的结构体数据写入到PLC
    /// </remarks>
    /// <typeparam name="T">结构体类型</typeparam>
    /// <param name="structValue">要写入的结构体实例</param>
    /// <param name="db">数据块编号</param>
    /// <param name="startByteAdr">起始字节地址，默认为0</param>
    /// <param name="cts">取消令牌</param>
    /// <returns>异步操作任务结果，由调用方处理可能发生的异常</returns>
    /// <exception cref="OperationCanceledException">取消此操作时抛出此异常</exception>
    /// <exception cref="WriteErrorException">写入异常</exception>
    Task WriteStructAsync<T>(T structValue, int db, int startByteAdr = 0, CancellationToken cts = default) where T : struct;

    /// <summary>
    /// 异步向PLC写入结构体数据
    /// </summary>
    /// <remarks>
    /// 这个方法适用于需要将C#中的结构体数据写入到PLC
    /// </remarks>
    /// <param name="structValue">要写入的结构体实例</param>
    /// <param name="db">数据块编号</param>
    /// <param name="startByteAdr">起始字节地址，默认为0</param>
    /// <param name="cts">取消令牌</param>
    /// <returns>异步操作任务结果，由调用方处理可能发生的异常</returns>
    /// <exception cref="OperationCanceledException">取消此操作时抛出此异常</exception>
    /// <exception cref="WriteErrorException">写入异常</exception>
    Task WriteStructAsync(object structValue, int db, int startByteAdr = 0, CancellationToken cts = default);

    /// <summary>
    /// 异步从PLC读取数据到C#类的实例
    /// </summary>
    /// <remarks>
    ///  这个方法适用于将数据块中的数据直接读入一个已定义的C#类中，处理复杂的数据结构
    /// </remarks>
    /// <param name="sourceClass">要填充数据的类的实例</param>
    /// <param name="db">数据块编号</param>
    /// <param name="startByteAdr">起始字节地址，默认为0</param>
    /// <param name="cts">取消令牌</param>
    /// <returns>异步操作任务结果，由调用方处理可能发生的异常</returns>
    /// <exception cref="OperationCanceledException">取消此操作时抛出此异常</exception>
    /// <exception cref="ReadErrorException">读取异常</exception>
    Task ReadClassAsync(object sourceClass, int db, int startByteAdr = 0, CancellationToken cts = default);

    /// <summary>
    /// 异步将 C# 类的实例数据写入PLC
    /// </summary>
    /// <remarks>
    /// 这个方法适用于通过将C#类实例的数据写入到PLC，处理复杂的数据结构
    /// </remarks>
    /// <param name="classValue">包含数据的类的实例</param>
    /// <param name="db">数据块编号</param>
    /// <param name="startByteAdr">起始字节地址，默认为0</param>
    /// <param name="cts">取消令牌</param>
    /// <returns>异步操作任务结果，由调用方处理可能发生的异常</returns>
    /// <exception cref="OperationCanceledException">取消此操作时抛出此异常</exception>
    /// <exception cref="WriteErrorException">写入异常</exception>
    Task WriteClassAsync(object classValue, int db, int startByteAdr = 0, CancellationToken cts = default);

    /// <summary>
    /// 异步从PLC读取多个变量
    /// </summary>
    /// <remarks>
    /// 这个方法适用于当需要在一个操作中读取多个不同的变量时使用，这些变量分布在不同的数据块或者地址，减少网络通信的次数
    /// </remarks>
    /// <param name="dataItems">包含要读取的所有数据项信息的列表</param>
    /// <param name="cts">取消令牌</param>
    /// <returns>异步操作任务结果，由调用方处理可能发生的异常</returns>
    /// <exception cref="OperationCanceledException">取消此操作时抛出此异常</exception>
    /// <exception cref="ReadErrorException">读取异常</exception>
    Task ReadMultipleVarsAsync(List<DataItem> dataItems, CancellationToken cts = default);

    /// <summary>
    /// 异步向PLC写入多个变量
    /// </summary>
    /// <remarks>
    /// 这个方法适用于当需要在一个操作中写入多个不同的变量时使用，这些变量分布在不同的数据块或者地址，减少网络通信的次数
    /// </remarks>
    /// <param name="dataItems">包含要写入的所有数据项信息的列表</param>
    /// <param name="cts">取消令牌</param>
    /// <returns>异步操作任务结果，由调用方处理可能发生的异常</returns>
    /// <exception cref="WriteErrorException">写入异常</exception>
    Task WriteMultipleVarsAsync(List<DataItem> dataItems, CancellationToken cts = default);
}

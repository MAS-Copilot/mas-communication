// =============================================================================
// Professional Automation Equipment Manufacturer.
// Website:       https://www.mas-automation.com/
//
// Copyright (c) MAS(厦门威光) Corporation. All rights reserved.
// =============================================================================

namespace MAS.Communication.McProtocol;

/// <summary>
/// 与三菱 MC 协议的 PLC 设备进行通信的接口
/// </summary>
public interface IMcProtocol : IProtocol {
    /// <summary>
    /// 异步从PLC读取字数据
    /// </summary>
    /// <param name="deviceType">指定设备类型</param>
    /// <param name="startAddress">起始地址</param>
    /// <param name="length">要读取的数据长度</param>
    /// <param name="cts">取消令牌</param>
    /// <returns>异步操作任务结果，包含读取值的整数数组</returns>
    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="ReadErrorException"></exception>
    Task<short[]> ReadWordsAsync(string deviceType, int startAddress, int length, CancellationToken cts = default);

    /// <summary>
    /// 异步从PLC读取字数据
    /// </summary>
    /// <typeparam name="T">数据类型： `short`、`int`、`float`、`double` </typeparam>
    /// <param name="deviceType">指定设备类型</param>
    /// <param name="startAddress">起始地址</param>
    /// <param name="length">要读取的数据长度</param>
    /// <param name="cts">取消令牌</param>
    /// <returns>异步操作任务结果，包含读取值的泛型数组</returns>
    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="ReadErrorException"></exception>
    Task<T[]> ReadWordsAsync<T>(string deviceType, int startAddress, int length, CancellationToken cts = default);

    /// <summary>
    /// 异步从PLC读取位数据
    /// </summary>
    /// <param name="deviceType">指定设备类型</param>
    /// <param name="startAddress">起始地址</param>
    /// <param name="length">要读取的位的数量</param>
    /// <param name="cts">取消令牌</param>
    /// <returns>异步操作任务结果，包含读取位状态的布尔值数组</returns>
    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="ReadErrorException"></exception>
    Task<bool[]> ReadBitsAsync(string deviceType, int startAddress, int length, CancellationToken cts = default);

    /// <summary>
    /// 异步写入字数据到PLC
    /// </summary>
    /// <param name="deviceType">指定设备类型</param>
    /// <param name="startAddress">起始地址</param>
    /// <param name="values">要写入的整数数组</param>
    /// <param name="cts">取消令牌</param>
    /// <returns>异步操作任务结果</returns>
    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="WriteErrorException"></exception>
    Task WriteWordsAsync(string deviceType, int startAddress, short[] values, CancellationToken cts = default);

    /// <summary>
    /// 异步写入字数据到PLC
    /// </summary>
    /// <typeparam name="T">数据类型： `short`、`int`、`float`、`double` </typeparam>
    /// <param name="deviceType">指定设备类型</param>
    /// <param name="startAddress">起始地址</param>
    /// <param name="values">要写入的数据数组</param>
    /// <param name="cts">取消令牌</param>
    /// <returns>异步操作任务结果</returns>
    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="WriteErrorException"></exception>
    Task WriteWordsAsync<T>(string deviceType, int startAddress, T[] values, CancellationToken cts = default);

    /// <summary>
    /// 异步写入位数据到PLC
    /// </summary>
    /// <param name="deviceType">指定设备类型</param>
    /// <param name="startAddress">起始地址</param>
    /// <param name="values">要写入的布尔值数组，表示位状态</param>
    /// <param name="cts">取消令牌</param>
    /// <returns>异步操作任务结果</returns>
    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="WriteErrorException"></exception>
    Task WriteBitsAsync(string deviceType, int startAddress, bool[] values, CancellationToken cts = default);

    /// <summary>
    /// 异步从 PLC 读取结构体数据
    /// </summary>
    /// <typeparam name="T">结构体类型</typeparam>
    /// <param name="deviceType">指定设备类型</param>
    /// <param name="startAddress">起始地址</param>
    /// <param name="cts">取消令牌</param>
    /// <returns>读取到的结构体实例</returns>
    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="ReadErrorException"></exception>
    Task<T> ReadStructAsync<T>(string deviceType, int startAddress, CancellationToken cts = default) where T : struct;

    /// <summary>
    /// 异步写入结构体数据到 PLC
    /// </summary>
    /// <typeparam name="T">结构体类型</typeparam>
    /// <param name="deviceType">指定设备类型</param>
    /// <param name="startAddress">起始地址</param>
    /// <param name="value">结构体实例</param>
    /// <param name="cts">取消令牌</param>
    /// <returns>异步操作任务</returns>
    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="WriteErrorException"></exception>
    Task WriteStructAsync<T>(string deviceType, int startAddress, T value, CancellationToken cts = default) where T : struct;
}

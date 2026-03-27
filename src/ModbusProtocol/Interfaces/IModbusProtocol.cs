// =============================================================================
// MAS.Communication
// https://www.mas-automation.com/
//
// Copyright 2026 MAS (厦门威光) Corporation
//
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.
// =============================================================================

namespace MAS.Communication.ModbusProtocol;

/// <summary>
/// Modbus 协议通信接口
/// </summary>
public interface IModbusProtocol : IProtocol {
    /// <summary>
    /// 异步读取位变量区域
    /// </summary>
    /// <param name="area">数据区域类型</param>
    /// <param name="startAddress">起始地址</param>
    /// <param name="count">读取的位数量</param>
    /// <param name="cts">取消令牌，用于取消异步操作</param>
    /// <returns>一个异步操作任务结果，布尔数组，表示各 bit 的值（true = 1, false = 0）</returns>
    /// <exception cref="ReadErrorException"></exception>
    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="TimeoutException"></exception>
    Task<bool[]> ReadBitsAsync(
        ModbusDataArea area,
        ushort startAddress,
        ushort count,
        CancellationToken cts = default);

    /// <summary>
    /// 异步读取 16 位寄存器区域
    /// </summary>
    /// <param name="area">数据区域类型</param>
    /// <param name="startAddress">起始地址</param>
    /// <param name="count">读取的寄存器数量</param>
    /// <param name="cts">取消令牌</param>
    /// <returns>一个异步操作任务结果，ushort数组，每个元素代表一个16位寄存器的值</returns>
    /// <exception cref="ReadErrorException"></exception>
    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="TimeoutException"></exception>
    Task<ushort[]> ReadRegistersAsync(
        ModbusDataArea area,
        ushort startAddress,
        ushort count,
        CancellationToken cts = default);

    /// <summary>
    /// 异步写入位变量到线圈区域
    /// </summary>
    /// <param name="startAddress">起始地址</param>
    /// <param name="values">要写入的布尔值数组（true = 置位，false = 复位）</param>
    /// <param name="cts">取消令牌</param>
    /// <returns>表示一个异步操作任务</returns>
    /// <exception cref="WriteErrorException"></exception>
    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="TimeoutException"></exception>
    Task WriteBitsAsync(
        ushort startAddress,
        bool[] values,
        CancellationToken cts = default);

    /// <summary>
    /// 异步写入 16 位寄存器数据
    /// </summary>
    /// <param name="startAddress">起始地址</param>
    /// <param name="values">要写入的 16 位值数组</param>
    /// <param name="cts">取消令牌</param>
    /// <returns>表示一个异步操作任务</returns>
    /// <exception cref="WriteErrorException"></exception>
    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="TimeoutException"></exception>
    Task WriteRegistersAsync(
        ushort startAddress,
        ushort[] values,
        CancellationToken cts = default);

    /// <summary>
    /// 异步从寄存器区域读取结构体数据
    /// </summary>
    /// <typeparam name="T">结构体类型</typeparam>
    /// <param name="area">数据区域类型（通常为 HoldingRegisters / InputRegisters）</param>
    /// <param name="startAddress">起始地址（寄存器地址）</param>
    /// <param name="cts">取消令牌</param>
    /// <returns>结构体实例</returns>
    /// <exception cref="ReadErrorException"></exception>
    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="TimeoutException"></exception>
    Task<T> ReadStructAsync<T>(
        ModbusDataArea area,
        ushort startAddress,
        CancellationToken cts = default) where T : struct;

    /// <summary>
    /// 异步将结构体数据写入寄存器区域
    /// </summary>
    /// <typeparam name="T">结构体类型</typeparam>
    /// <param name="startAddress">起始地址（寄存器地址）</param>
    /// <param name="value">结构体实例</param>
    /// <param name="cts">取消令牌</param>
    /// <returns>异步操作任务</returns>
    /// <exception cref="WriteErrorException"></exception>
    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="TimeoutException"></exception>
    Task WriteStructAsync<T>(
        ushort startAddress,
        T value,
        CancellationToken cts = default) where T : struct;
}

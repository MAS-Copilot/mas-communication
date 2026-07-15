// =============================================================================
// MAS.Communication
// https://www.mas-automation.com/
//
// Copyright 2026 MAS (厦门威光) Corporation
//
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.
// =============================================================================

namespace MAS.Communication.McProtocol;

/// <summary>
/// MC 协议响应帧读取帮助类
/// </summary>
/// <remarks>
/// TCP 是字节流，一帧 MC 响应可能被拆成多个分段到达。此处按协议帧结构读满整帧：
/// 先读满定长帧头，再根据帧头中的「响应数据长度」字段读够剩余字节，
/// 避免用单次读的长度判断消息边界而拿到残帧、进而让后续读取永久错位（desync）。
/// </remarks>
internal static class McFrameReader {
    /// <summary>
    /// 获取指定帧类型的响应帧头长度（字节）
    /// </summary>
    /// <remarks>
    /// MC1E 响应帧头为「子头(1) + 结束代码(1)」共 2 字节；
    /// MC3E 为 11 字节、MC4E 为 15 字节，两者帧头内均含结束代码与响应数据长度字段。
    /// </remarks>
    public static int GetResponseHeaderLength(McFrame frameType) {
        return frameType switch {
            McFrame.MC1E => 2,
            McFrame.MC3E => 11,
            McFrame.MC4E => 15,
            _ => throw new Exception("Message frame not supported."),
        };
    }

    /// <summary>
    /// 从流中读取一帧完整的 MC 响应
    /// </summary>
    /// <param name="stream">网络流</param>
    /// <param name="frameType">协议帧类型</param>
    /// <param name="expectedDataLength">
    /// MC1E 正常响应的预期数据字节数（由请求推算）。MC1E 响应头没有长度字段，
    /// 只能依据请求推算数据长度；MC3E/MC4E 从帧头的长度字段计算，忽略此参数。
    /// </param>
    /// <param name="cts">取消令牌</param>
    /// <returns>完整的响应帧字节数组</returns>
    /// <exception cref="ConnectionException">读到 0 字节（对端已关闭连接/EOF）时抛出</exception>
    /// <exception cref="OperationCanceledException">读取被取消或超时</exception>
    public static async Task<byte[]> ReadResponseFrameAsync(
        Stream stream,
        McFrame frameType,
        int expectedDataLength,
        CancellationToken cts = default) {

        int headerLength = GetResponseHeaderLength(frameType);

        // 先按定长读满帧头，避免 TCP 分段导致的残帧
        byte[] header = await ReadExactlyAsync(stream, headerLength, cts).ConfigureAwait(false);

        int remaining = frameType switch {
            // MC3E/MC4E 帧头末尾为「响应数据长度」字段，其值 = 结束代码(2) + 数据字节数，
            // 结束代码已包含在帧头内，故仍需读取的数据字节数为「长度字段值 - 2」。
            McFrame.MC3E or McFrame.MC4E =>
                (header[headerLength - 4] | (header[headerLength - 3] << 8)) - 2,
            // MC1E 响应头无长度字段：结束代码（第 2 字节）为 0 表示正常，随后为预期数据；
            // 非 0 表示异常，随后固定为 1 字节异常代码。
            McFrame.MC1E => header[1] == 0x00 ? expectedDataLength : 1,
            _ => throw new Exception("Message frame not supported."),
        };

        if (remaining <= 0) {
            return header;
        }

        byte[] body = await ReadExactlyAsync(stream, remaining, cts).ConfigureAwait(false);

        byte[] frameBytes = new byte[headerLength + remaining];
        Buffer.BlockCopy(header, 0, frameBytes, 0, headerLength);
        Buffer.BlockCopy(body, 0, frameBytes, headerLength, remaining);
        return frameBytes;
    }

    /// <summary>
    /// 从流中精确读取指定字节数
    /// </summary>
    /// <remarks>
    /// 循环读取直至读满 <paramref name="count"/> 字节。读到 0 字节表示对端已关闭连接（EOF），
    /// 判定为断线并抛出 <see cref="ConnectionException"/>；读取被取消/超时则抛出
    /// <see cref="OperationCanceledException"/>，两者语义严格区分，取消不当作断线。
    /// </remarks>
    private static async Task<byte[]> ReadExactlyAsync(Stream stream, int count, CancellationToken cts) {
        byte[] buffer = new byte[count];
        int offset = 0;

        while (offset < count) {
#if NETFRAMEWORK
            int read = await stream.ReadAsync(buffer, offset, count - offset, cts).ConfigureAwait(false);
#else
            int read = await stream.ReadAsync(buffer.AsMemory(offset, count - offset), cts).ConfigureAwait(false);
#endif
            if (read == 0) {
                // 读到 0 字节说明对端已关闭连接（TCP 进入半开/EOF 状态），判定为断线。
                throw new ConnectionException("The connection has been closed by the remote host.");
            }

            offset += read;
        }

        return buffer;
    }
}

// =============================================================================
// MAS.Communication
// https://www.mas-automation.com/
//
// Copyright 2026 MAS (厦门威光) Corporation
//
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.
// =============================================================================

using MAS.Communication;
using MAS.Communication.McProtocol;

namespace MAS.CommunicationUnitTest.McProtocol;

/// <summary>
/// McFrameReader 读帧逻辑单元测试（不依赖真实 PLC）。
/// 重点验证：TCP 分段到达的响应能被按帧结构正确拼接为完整帧，
/// 以及读到 0 字节（对端关闭）时判定为断线、取消不被误判为断线。
/// </summary>
[TestClass]
public class McFrameReaderTest {
    /// <summary>
    /// 构造一帧完整的 MC3E/MC4E 响应：定长帧头（含数据长度字段与结束代码）+ 数据。
    /// </summary>
    private static byte[] BuildSelfDescribingFrame(McFrame frame, byte[] data) {
        int headerLength = McFrameReader.GetResponseHeaderLength(frame);
        int rsCount = data.Length + 2; // 数据长度字段 = 结束代码(2) + 数据

        byte[] frameBytes = new byte[headerLength + data.Length];

        // 子头
        frameBytes[0] = frame == McFrame.MC3E ? (byte)0xD0 : (byte)0xD4;

        // 数据长度字段位于帧头末尾第 4、3 字节
        frameBytes[headerLength - 4] = (byte)rsCount;
        frameBytes[headerLength - 3] = (byte)(rsCount >> 8);

        // 结束代码 0x0000（正常）位于帧头末尾第 2、1 字节
        frameBytes[headerLength - 2] = 0x00;
        frameBytes[headerLength - 1] = 0x00;

        Buffer.BlockCopy(data, 0, frameBytes, headerLength, data.Length);
        return frameBytes;
    }

    [TestMethod]
    [DataRow(McFrame.MC3E, 1)]
    [DataRow(McFrame.MC3E, 2)]
    [DataRow(McFrame.MC3E, 3)]
    [DataRow(McFrame.MC3E, 7)]
    [DataRow(McFrame.MC3E, 256)]
    [DataRow(McFrame.MC4E, 1)]
    [DataRow(McFrame.MC4E, 2)]
    [DataRow(McFrame.MC4E, 5)]
    [DataRow(McFrame.MC4E, 256)]
    public async Task 分段到达的自描述帧能被正确拼接为完整帧(McFrame frame, int chunkSize) {
        byte[] data = [0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88];
        byte[] expected = BuildSelfDescribingFrame(frame, data);

        using var stream = new ChunkedReadStream(expected, chunkSize);
        byte[] actual = await McFrameReader.ReadResponseFrameAsync(stream, frame, expectedDataLength: 0, TestContext.CancellationTokenSource.Token);

        CollectionAssert.AreEqual(expected, actual,
            $"帧类型 {frame} 在每次最多读 {chunkSize} 字节的分段下未能正确拼接完整帧");
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(256)]
    public async Task 分段到达的MC1E正常帧能按预期数据长度拼接完整(int chunkSize) {
        // MC1E 响应头：子头(0x80) + 结束代码(0x00 正常)，随后为预期数据字节
        byte[] data = [0xAA, 0xBB, 0xCC, 0xDD];
        byte[] expected = [0x80, 0x00, .. data];

        using var stream = new ChunkedReadStream(expected, chunkSize);
        byte[] actual = await McFrameReader.ReadResponseFrameAsync(
            stream, McFrame.MC1E, expectedDataLength: data.Length, TestContext.CancellationTokenSource.Token);

        CollectionAssert.AreEqual(expected, actual);
    }

    [TestMethod]
    public async Task MC1E异常帧读取1字节异常代码() {
        // 结束代码非 0 时，随后固定为 1 字节异常代码，与预期数据长度无关
        byte[] expected = [0x80, 0x51, 0xAB];

        using var stream = new ChunkedReadStream(expected, 1);
        byte[] actual = await McFrameReader.ReadResponseFrameAsync(
            stream, McFrame.MC1E, expectedDataLength: 100, TestContext.CancellationTokenSource.Token);

        CollectionAssert.AreEqual(expected, actual);
    }

    [TestMethod]
    public async Task 读到0字节判定为断线并抛出连接异常() {
        // 帧头尚未读满即 EOF（对端关闭）
        byte[] partial = [0xD0, 0x00, 0x00, 0xFF]; // 不足 MC3E 的 11 字节帧头

        using var stream = new ChunkedReadStream(partial, 8);
        _ = await Assert.ThrowsExactlyAsync<ConnectionException>(async () =>
            await McFrameReader.ReadResponseFrameAsync(stream, McFrame.MC3E, expectedDataLength: 0, TestContext.CancellationTokenSource.Token));
    }

    [TestMethod]
    public async Task 数据段中途EOF判定为断线并抛出连接异常() {
        // 帧头完整、声明还有 4 字节数据，但数据只到达 2 字节即 EOF
        byte[] frame = BuildSelfDescribingFrame(McFrame.MC3E, [0x11, 0x22, 0x33, 0x44]);
        byte[] truncated = frame[..(frame.Length - 2)];

        using var stream = new ChunkedReadStream(truncated, 8);
        _ = await Assert.ThrowsExactlyAsync<ConnectionException>(async () =>
            await McFrameReader.ReadResponseFrameAsync(stream, McFrame.MC3E, expectedDataLength: 0, TestContext.CancellationTokenSource.Token));
    }

    [TestMethod]
    public async Task 取消令牌触发时抛出取消异常而非连接异常() {
        byte[] frame = BuildSelfDescribingFrame(McFrame.MC3E, [0x11, 0x22]);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        using var stream = new ChunkedReadStream(frame, 4);
        _ = await Assert.ThrowsExactlyAsync<OperationCanceledException>(async () =>
            await McFrameReader.ReadResponseFrameAsync(stream, McFrame.MC3E, expectedDataLength: 0, cts.Token));
    }

    /// <summary>
    /// 模拟 TCP 分段的只读流：每次 Read 最多返回 <c>chunkSize</c> 字节，数据耗尽后返回 0（EOF）。
    /// </summary>
    private sealed class ChunkedReadStream(byte[] data, int chunkSize) : Stream {
        private readonly byte[] _data = data;
        private readonly int _chunkSize = chunkSize;
        private int _position;

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) {
            cancellationToken.ThrowIfCancellationRequested();

            int available = _data.Length - _position;
            if (available <= 0) {
                return ValueTask.FromResult(0);
            }

            int toCopy = Math.Min(Math.Min(_chunkSize, buffer.Length), available);
            _data.AsMemory(_position, toCopy).CopyTo(buffer);
            _position += toCopy;
            return ValueTask.FromResult(toCopy);
        }

        public override int Read(byte[] buffer, int offset, int count) {
            int available = _data.Length - _position;
            if (available <= 0) {
                return 0;
            }

            int toCopy = Math.Min(Math.Min(_chunkSize, count), available);
            Array.Copy(_data, _position, buffer, offset, toCopy);
            _position += toCopy;
            return toCopy;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => _data.Length;
        public override long Position { get => _position; set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    public TestContext TestContext { get; set; }
}

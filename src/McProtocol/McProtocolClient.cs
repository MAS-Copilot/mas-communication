// =============================================================================
// MAS.Communication
// https://www.mas-automation.com/
//
// Copyright 2026 MAS (厦门威光) Corporation
//
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.
// =============================================================================

using System.Net.Sockets;

namespace MAS.Communication.McProtocol;

internal class McProtocolClient(IMcCommunicationConfig config) : IMcProtocol {
    private readonly IMcCommunicationConfig _config = config;

    #region 私有字段

    private bool _disposed;
    private TcpClient? _client;
    private NetworkStream? _stream;
    private readonly SemaphoreSlim _streamLock = new(1, 1);
    private readonly SemaphoreSlim _connectLock = new(1, 1);

    #endregion

    public event EventHandler? Disposed;

    public CommProtocol ProtocolType => CommProtocol.MC;

    public ICommunicationConfig Configuration => _config;

    public bool CheckConnection() => IsSocketAlive();

    public async Task ConnectAsync(CancellationToken cts = default) {
        try {
            await OpenAsync(cts).ConfigureAwait(false);
        } catch (OperationCanceledException) {
            throw;
        } catch (Exception ex) {
            throw new ConnectionException($"PLC connection failed: {ex.Message}", ex);
        }
    }

    public void Disconnect() {
        DoDisconnect();
    }

    public void Dispose() {
        if (_disposed) {
            return;
        }

        DoDisconnect();
        _disposed = true;

        Disposed?.Invoke(this, EventArgs.Empty);
    }

    public async Task<bool> ProbeConnectionAsync(CancellationToken cts = default) {
        try {
            await OpenAsync(cts).ConfigureAwait(false);
            return true;
        } catch (OperationCanceledException) {
            throw;
        } catch {
            return false;
        } finally {
            Disconnect();
        }
    }

    public async Task<bool[]> ReadBitsAsync(string deviceType, int startAddress, int length, CancellationToken cts = default) {
        try {
            int byteLength = (length / 16 + ((length % 16 > 0) ? 1 : 0)) * 2;
            var bytes = await ReadDeviceBlockAsync(
                MitsubishiHelper.ParsePlcDeviceType(deviceType),
                startAddress,
                byteLength,
                cts
            ).ConfigureAwait(false);

            if (bytes.Length < byteLength) {
                throw new ReadErrorException(
                    $"Not enough data read from PLC. Expected at least {byteLength} bytes, but got {bytes.Length} bytes.");
            }

            cts.ThrowIfCancellationRequested();
            bool[] bits = new bool[length];
            for (int i = 0; i < length; i++) {
                bits[i] = (bytes[i / 8] >> (i % 8) & 1) == 1;
            }

            return bits;
        } catch (OperationCanceledException) {
            throw;
        } catch (Exception ex) {
            throw new ReadErrorException(
                $"Error reading bits from PLC. Device: {deviceType}, StartAddress: {startAddress}, Length: {length}.", ex);
        }
    }

    public async Task<T> ReadStructAsync<T>(string deviceType, int startAddress, CancellationToken cts = default) where T : struct {
        try {
            int numBytes = StructBinaryHelper.GetStructSize(typeof(T));
            int numRegisters = (int)Math.Ceiling(numBytes / 2.0);

            byte[] bytes = await ReadDeviceBlockAsync(
                PlcDeviceType.D,
                startAddress,
                numRegisters,
                cts
            ).ConfigureAwait(false);

            if (bytes.Length < numBytes) {
                throw new ReadErrorException(
                    $"Insufficient data. Expected {numBytes}, got {bytes.Length}.");
            }

            return StructBinaryHelper.BytesToStruct<T>(bytes);
        } catch (OperationCanceledException) {
            throw;
        } catch (Exception ex) {
            throw new ReadErrorException(
                $"Failed to read structure from PLC. Type: {typeof(T).FullName} StartAddress: {startAddress}.", ex);
        }
    }

    public async Task<short[]> ReadWordsAsync(string deviceType, int startAddress, int length, CancellationToken cts = default) {
        try {
            int byteLength = length * 2;
            var bytes = await ReadDeviceBlockAsync(
                MitsubishiHelper.ParsePlcDeviceType(deviceType),
                startAddress,
                byteLength,
                cts
            ).ConfigureAwait(false);

            if (bytes.Length < byteLength) {
                throw new ReadErrorException(
                    $"Not enough data read from PLC. Expected at least {byteLength} bytes, but got {bytes.Length} bytes.");
            }

            cts.ThrowIfCancellationRequested();
            var words = new short[length];
            for (int i = 0; i < length; i++) {
                words[i] = (short)(bytes[i * 2] | (bytes[i * 2 + 1]) << 8);
            }

            return words;
        } catch (OperationCanceledException) {
            throw;
        } catch (Exception ex) {
            throw new ReadErrorException(
                $"Error reading words from PLC. Device: {deviceType}, StartAddress: {startAddress}, Length: {length}", ex);
        }
    }

    public async Task<T[]> ReadWordsAsync<T>(string deviceType, int startAddress, int length, CancellationToken cts = default) {
        try {
            int byteLength = length * StructBinaryHelper.GetTypeByteLength(typeof(T));
            var bytes = await ReadDeviceBlockAsync(
                MitsubishiHelper.ParsePlcDeviceType(deviceType),
                startAddress,
                byteLength,
                cts
            ).ConfigureAwait(false);

            if (bytes.Length < byteLength) {
                throw new ReadErrorException(
                    $"Not enough data read from PLC. Expected at least {byteLength} bytes, but got {bytes.Length} bytes.");
            }

            T[] result = new T[length];
            for (int i = 0; i < length; i++) {
                byte[] dataBytes = new byte[StructBinaryHelper.GetTypeByteLength(typeof(T))];
                Array.Copy(bytes, i * dataBytes.Length, dataBytes, 0, dataBytes.Length);
                result[i] = (T)StructBinaryHelper.GetValueFromBytes(dataBytes, typeof(T));
            }

            return result;
        } catch (OperationCanceledException) {
            throw;
        } catch (Exception ex) {
            throw new ReadErrorException(
                $"Error reading data from PLC. Device: {deviceType}, StartAddress: {startAddress}, Length: {length}.", ex);
        }
    }

    public async Task<bool> TryReconnectAsync(int maxAttempts, int retryDelay = 1000, CancellationToken cts = default) {
        int attempt = 0;
        while (attempt < maxAttempts) {
            try {
                await OpenAsync(cts).ConfigureAwait(false);
                return true;
            } catch (OperationCanceledException) {
                throw;
            } catch {
                await Task.Delay(retryDelay, cts);
            }

            attempt++;
        }

        return false;
    }

    public async Task WriteBitsAsync(string deviceType, int startAddress, bool[] values, CancellationToken cts = default) {
        try {
            int numBits = values.Length;
            int byteLength = (numBits / 16 + ((numBits % 16 > 0) ? 1 : 0)) * 2;
            byte[] bytes = new byte[byteLength];

            for (int i = 0; i < numBits; i++) {
                int wordIndex = i / 16;
                int bitInWord = i % 16;
                int byteIndex = wordIndex * 2 + (bitInWord / 8);
                int bitInByte = bitInWord % 8;

                if (values[i]) {
                    bytes[byteIndex] |= (byte)(1 << bitInByte);
                } else {
                    bytes[byteIndex] &= (byte)~(1 << bitInByte);
                }
            }

            _ = await WriteDeviceBlockAsync(
                MitsubishiHelper.ParsePlcDeviceType(deviceType),
                startAddress,
                byteLength / 2,
                bytes,
                cts
            ).ConfigureAwait(false);
        } catch (OperationCanceledException) {
            throw;
        } catch (Exception ex) {
            throw new WriteErrorException(
                $"Error writing bits to PLC. Device: {deviceType}, StartAddress: {startAddress}, Length: {values.Length}.", ex);
        }
    }

    public async Task WriteStructAsync<T>(string deviceType, int startAddress, T value, CancellationToken cts = default) where T : struct {
        try {
            byte[] bytes = StructBinaryHelper.StructToBytes(value);
            int numRegisters = (int)Math.Ceiling(bytes.Length / 2.0);

            _ = await WriteDeviceBlockAsync(
                MitsubishiHelper.ParsePlcDeviceType(deviceType),
                startAddress,
                numRegisters,
                bytes,
                cts
            ).ConfigureAwait(false);
        } catch (OperationCanceledException) {
            throw;
        } catch (Exception ex) {
            throw new WriteErrorException(
                $"Failed to write structure to PLC. StructType: {typeof(T).FullName} StartAddress: {startAddress}.", ex);
        }
    }

    public async Task WriteWordsAsync(string deviceType, int startAddress, short[] values, CancellationToken cts = default) {
        try {
            int byteLength = values.Length * 2;
            byte[] bytes = new byte[byteLength];

            for (int i = 0; i < values.Length; i++) {
                byte[] shortBytes = BitConverter.GetBytes(values[i]);
                Array.Copy(shortBytes, 0, bytes, i * 2, 2);
            }

            _ = await WriteDeviceBlockAsync(
                MitsubishiHelper.ParsePlcDeviceType(deviceType),
                startAddress,
                byteLength / 2,
                bytes,
                cts
            ).ConfigureAwait(false);
        } catch (OperationCanceledException) {
            throw;
        } catch (Exception ex) {
            throw new WriteErrorException(
                $"Error writing words to PLC. Device: {deviceType}, StartAddress: {startAddress}, Length: {values.Length}.", ex);
        }
    }

    public async Task WriteWordsAsync<T>(string deviceType, int startAddress, T[] values, CancellationToken cts = default) {
        try {
            var length = StructBinaryHelper.GetTypeByteLength(typeof(T));
            var byteLength = values.Length * length;
            var bytes = new byte[byteLength];

            for (int i = 0; i < values.Length; i++) {
                var valueBytes = StructBinaryHelper.GetBytes(values[i]!);
                Array.Copy(valueBytes, 0, bytes, i * length, length);
            }

            _ = await WriteDeviceBlockAsync(
                MitsubishiHelper.ParsePlcDeviceType(deviceType),
                startAddress,
                byteLength / 2,
                bytes,
                cts
            ).ConfigureAwait(false);
        } catch (OperationCanceledException) {
            throw;
        } catch (Exception ex) {
            throw new WriteErrorException(
                $"Error writing data to PLC. Device: {deviceType}, StartAddress: {startAddress}, Length: {values.Length}.", ex);
        }
    }

    #region 私有方法

    private bool IsSocketAlive() {
        try {
            var socket = _client?.Client;
            if (socket == null || !socket.Connected) {
                return false;
            }

            // socket.Connected 反映的是最近一次收发时的状态，对半开连接并不可靠；
            // 这里再用 Poll + Available 识别对端已关闭的情况：Poll 返回可读且
            // Available == 0，说明收到了对端的 FIN（连接已断），判定为不可用。
            if (socket.Poll(0, SelectMode.SelectRead) && socket.Available == 0) {
                return false;
            }

            return true;
        } catch {
            return false;
        }
    }

    private async Task OpenAsync(CancellationToken cts = default) {
        await _connectLock.WaitAsync(cts).ConfigureAwait(false);

        try {
            if (CheckConnection()) {
                return;
            }

            InvalidateConnection();

            TcpClient client = new();
            await ConnectTcpClientAsync(client, _config.Ip, _config.Port, cts).ConfigureAwait(false);

            ConfigureKeepAlive(client);

            _client = client;
            _stream = client.GetStream();
        } catch (OperationCanceledException) {
            InvalidateConnection();
            throw;
        } catch {
            InvalidateConnection();
            throw;
        } finally {
            _ = _connectLock.Release();
        }
    }

    private static void ConfigureKeepAlive(TcpClient client) {
        Socket socket = client.Client;

        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

#if !NETFRAMEWORK
        if (!OperatingSystem.IsWindows()) {
            return;
        }
#endif

        byte[] keepAliveValues = new byte[12];
        BitConverter.GetBytes(1u).CopyTo(keepAliveValues, 0);
        BitConverter.GetBytes(45000u).CopyTo(keepAliveValues, 4);
        BitConverter.GetBytes(5000u).CopyTo(keepAliveValues, 8);

        _ = socket.IOControl(IOControlCode.KeepAliveValues, keepAliveValues, null);
    }

    private async Task<int> WriteDeviceBlockAsync(PlcDeviceType type, int address, int size, byte[] data, CancellationToken cts = default) {
        var mcCommand = new McCommand(_config.ProtocolFrame);
        var (sdCommand, length) = _config.ProtocolFrame switch {
            McFrame.MC3E => CommandHelper.BuildMc3E(type, address, size, mcCommand, data),
            McFrame.MC4E => CommandHelper.BuildMc4E(type, address, size, mcCommand, data),
            McFrame.MC1E => CommandHelper.BuildMc1E(address, size, mcCommand, data),
            _ => throw new Exception("Message frame not supported"),
        };

        // 写请求的响应仅为确认帧（无数据部分），MC1E 预期数据长度为 0。
        byte[] rtResponse = await TryExecutionAsync(
            sdCommand, length, expectedDataLength: 0, cts: cts
        ).ConfigureAwait(false);

        return mcCommand.SetResponse(rtResponse);
    }

    private async Task<byte[]> ReadDeviceBlockAsync(PlcDeviceType type, int address, int size, CancellationToken cts = default) {
        var mcCommand = new McCommand(_config.ProtocolFrame);
        var (sdCommand, length) = _config.ProtocolFrame switch {
            McFrame.MC3E => CommandHelper.BuildMc3E(type, address, size, mcCommand),
            McFrame.MC4E => CommandHelper.BuildMc4E(type, address, size, mcCommand),
            McFrame.MC1E => CommandHelper.BuildMc1E(address, size, mcCommand),
            _ => throw new Exception("Message frame not supported"),
        };

        // 读请求响应含数据部分。MC1E 响应头无长度字段，需按请求推算数据字节数
        // （与 MC3E/MC4E 一致，返回 size 个点、每点 2 字节）；MC3E/MC4E 会改用帧头长度字段。
        byte[] rtResponse = await TryExecutionAsync(
            sdCommand, length, expectedDataLength: size * 2, cts: cts
        ).ConfigureAwait(false);

        _ = mcCommand.SetResponse(rtResponse);
        byte[] rtData = mcCommand.Response;

        return rtData;
    }

    private async Task<byte[]> TryExecutionAsync(
        byte[] command,
        int minlength,
        int expectedDataLength,
        int maxRetries = 10,
        TimeSpan? retryDelay = null,
        CancellationToken cts = default) {

        byte[] response;
        int retryCount = 0;

        do {
            // ExecuteAsync 已按帧结构读满整帧；若对端关闭/传输异常会抛出并失效连接，
            // 这里得到的始终是一帧完整响应（长度不小于帧头）。
            response = await ExecuteAsync(command, expectedDataLength, cts).ConfigureAwait(false);
            retryCount++;

            if (MitsubishiHelper.IsIncorrectResponse(_config.ProtocolFrame, response, minlength)) {
                if (retryCount >= maxRetries) {
                    throw new ReadErrorException($"Could not get the correct value from the PLC");
                }

                if (retryDelay.HasValue) {
                    await Task.Delay(retryDelay.Value, cts).ConfigureAwait(false);
                }
            }
        } while (MitsubishiHelper.IsIncorrectResponse(_config.ProtocolFrame, response, minlength));

        return response;
    }

    private async Task<byte[]> ExecuteAsync(byte[] command, int expectedDataLength, CancellationToken cts = default) {
        if (_stream is not NetworkStream stream) {
            throw new InvalidOperationException("The network flow is uninitialized");
        }

        await _streamLock.WaitAsync(cts).ConfigureAwait(false);
        try {
            await WriteToStreamAsync(stream, command, cts).ConfigureAwait(false);
            await FlushStreamAsync(stream, cts).ConfigureAwait(false);

            // TCP 是字节流，一帧 MC 响应可能被拆成多个分段到达。必须按协议帧结构读满
            // 整帧，不能用单次读的长度判断消息边界，否则会拿到残帧并让后续读取永久错位。
            return await McFrameReader.ReadResponseFrameAsync(
                stream, _config.ProtocolFrame, expectedDataLength, cts).ConfigureAwait(false);
        } catch (OperationCanceledException) {
            // 读写超时或被主动取消不属于断线，保持内部连接状态不变，直接向上传播。
            throw;
        } catch {
            // 发送/接收阶段出现 EOF 或 IO/Socket 等传输异常，均视为连接已失效：
            // 复位内部连接状态后再向上传播，确保随后重连时能真正重建连接，
            // 也避免残帧留下的脏字节让之后每次读取都错位、故障蔓延。
            InvalidateConnection();
            throw;
        } finally {
            _ = _streamLock.Release();
        }
    }

    private static Task WriteToStreamAsync(NetworkStream stream, byte[] buffer, CancellationToken cts) {
#if NETFRAMEWORK
        return stream.WriteAsync(buffer, 0, buffer.Length, cts);
#else
        return stream.WriteAsync(buffer, cts).AsTask();
#endif
    }

    private static Task FlushStreamAsync(NetworkStream stream, CancellationToken cts) {
#if NETFRAMEWORK
        return stream.FlushAsync();
#else
        return stream.FlushAsync(cts);
#endif
    }

    private static async Task ConnectTcpClientAsync(TcpClient client, string ip, int port, CancellationToken cts) {
#if NETFRAMEWORK
        cts.ThrowIfCancellationRequested();
        await client.ConnectAsync(ip, port).ConfigureAwait(false);
        cts.ThrowIfCancellationRequested();
#else
        await client.ConnectAsync(ip, port, cts).ConfigureAwait(false);
#endif
    }

    private void DoDisconnect() {
        InvalidateConnection();
    }

    private void InvalidateConnection() {
        try {
            _stream?.Dispose();
        } catch {

        }

        try {
            _client?.Dispose();
        } catch {

        }

        _stream = null;
        _client = null;
    }

    #endregion
}

// =============================================================================
// MAS.Communication
// https://www.mas-automation.com/
//
// Copyright 2026 MAS (厦门威光) Corporation
//
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.
// =============================================================================

using System.Buffers;
using System.Net.Sockets;

namespace MAS.Communication.ModbusProtocol;

internal class ModbusProtocolTcp(IModbusCommunicationConfig configuration) : IModbusProtocol {
    private readonly IModbusCommunicationConfig _config = configuration;

    #region 私有字段

    private bool _disposed;
    private TcpClient? _client;
    private NetworkStream? _stream;
    private readonly SemaphoreSlim _streamLock = new(1, 1);
    private ushort _transactionId;
    private readonly SemaphoreSlim _connectLock = new(1, 1);

    #endregion

    public event EventHandler? Disposed;

    public CommProtocol ProtocolType => CommProtocol.ModbusTcp;

    public ICommunicationConfig Configuration => _config;

    public bool CheckConnection() => IsSocketAlive();

    public async Task ConnectAsync(CancellationToken cts = default) {
        try {
            await OpenAsync(cts).ConfigureAwait(false);
        } catch (OperationCanceledException) {
            throw;
        } catch (Exception ex) {
            throw new ConnectionException($"Modbus TCP connection failed: {ex.Message}", ex);
        }
    }

    public void Disconnect() => DoDisconnect();

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
            await ConnectAsync(cts).ConfigureAwait(false);
            return true;
        } catch (OperationCanceledException) {
            throw;
        } catch {
            return false;
        } finally {
            Disconnect();
        }
    }

    public async Task<bool[]> ReadBitsAsync(ModbusDataArea area, ushort startAddress, ushort count, CancellationToken cts = default) {
        using var execCts = CreateCts(true, cts);
        CancellationToken execToken = execCts?.Token ?? cts;

        try {
            if (area is not ModbusDataArea.Coils and not ModbusDataArea.DiscreteInputs) {
                throw new ReadErrorException($"Invalid area for ReadBitsAsync: {area}.");
            }

            if (count is 0 or > ModbusTcpConstants.MAX_READ_BITS_PER_REQUEST) {
                throw new ReadErrorException($"Invalid count: {count}. Allowed range is 1..{ModbusTcpConstants.MAX_READ_BITS_PER_REQUEST}.");
            }

            byte fc = area == ModbusDataArea.Coils
                ? ModbusTcpConstants.FC_READ_COILS
                : ModbusTcpConstants.FC_READ_DISCRETE_INPUTS;

            ushort addr = ModbusAddressHelper.NormalizeAddress(startAddress, _config.UseOneBasedAddress);

            ushort tid = NextTransactionId();
            byte[] request = ModbusTcpFrameBuilder.BuildReadBitsRequest(tid, _config.UnitId, fc, addr, count);

            byte[] response = await ExecuteAsync(request, execToken).ConfigureAwait(false);

            return ModbusTcpFrameParser.ParseReadBitsResponse(response, tid, _config.UnitId, fc, count);
        } catch (OperationCanceledException ex) {
            if (!cts.IsCancellationRequested && execCts != null && execCts.IsCancellationRequested) {
                throw new TimeoutException($"Modbus TCP read bits timeout. Area={area}, StartAddress={startAddress}, Count={count}.", ex);
            }

            throw;
        } catch (Exception ex) when (ex is not ReadErrorException) {
            throw new ReadErrorException($"Error reading bits. Area={area}, StartAddress={startAddress}, Count={count}.", ex);
        }
    }

    public async Task<ushort[]> ReadRegistersAsync(ModbusDataArea area, ushort startAddress, ushort count, CancellationToken cts = default) {
        using var execCts = CreateCts(true, cts);
        CancellationToken execToken = execCts?.Token ?? cts;

        try {
            if (area is not ModbusDataArea.HoldingRegisters and not ModbusDataArea.InputRegisters) {
                throw new ReadErrorException($"Invalid area for ReadRegistersAsync: {area}.");
            }

            if (count is 0 or > ModbusTcpConstants.MAX_READ_REGISTERS_PER_REQUEST) {
                throw new ReadErrorException($"Invalid count: {count}. Allowed range is 1..{ModbusTcpConstants.MAX_READ_REGISTERS_PER_REQUEST}.");
            }

            byte fc = area == ModbusDataArea.HoldingRegisters
                ? ModbusTcpConstants.FC_READ_HOLDING_REGISTERS
                : ModbusTcpConstants.FC_READ_INPUT_REGISTERS;

            ushort addr = ModbusAddressHelper.NormalizeAddress(startAddress, _config.UseOneBasedAddress);

            ushort tid = NextTransactionId();
            byte[] request = ModbusTcpFrameBuilder.BuildReadRegistersRequest(tid, _config.UnitId, fc, addr, count);

            byte[] response = await ExecuteAsync(request, execToken).ConfigureAwait(false);

            return ModbusTcpFrameParser.ParseReadRegistersResponse(response, tid, _config.UnitId, fc, count);
        } catch (OperationCanceledException ex) {
            if (!cts.IsCancellationRequested && execCts != null && execCts.IsCancellationRequested) {
                throw new TimeoutException($"Modbus TCP read registers timeout. Area={area}, StartAddress={startAddress}, Count={count}.", ex);
            }

            throw;
        } catch (Exception ex) when (ex is not ReadErrorException) {
            throw new ReadErrorException($"Error reading registers. Area={area}, StartAddress={startAddress}, Count={count}.", ex);
        }
    }

    public async Task<T> ReadStructAsync<T>(ModbusDataArea area, ushort startAddress, CancellationToken cts = default) where T : struct {
        try {
            int byteCount = StructBinaryHelper.GetStructSize(typeof(T));
            int registerCount = (byteCount + 1) / 2;

            if (registerCount is <= 0 or > ModbusTcpConstants.MAX_READ_REGISTERS_PER_REQUEST) {
                throw new ReadErrorException(
                    $"Struct size too large to read in one request. Type={typeof(T).FullName}, RegisterCount={registerCount}.");
            }

            ushort[] registers = await ReadRegistersAsync(
                area,
                startAddress,
                (ushort)registerCount,
                cts: cts
            ).ConfigureAwait(false);

            byte[] bytes = ModbusRegisterBinaryHelper.RegistersToLittleEndianBytes(
                registers, byteCount, _config.ByteOrder, _config.WordOrder);

            object? obj = StructBinaryHelper.BytesToStruct(bytes, typeof(T));

            if (obj is not T t) {
                throw new ReadErrorException(
                    $"Failed to convert bytes to struct. Type={typeof(T).FullName}, StartAddress={startAddress}");
            }

            return t;
        } catch (OperationCanceledException) {
            throw;
        } catch (TimeoutException) {
            throw;
        } catch (Exception ex) when (ex is not ReadErrorException) {
            throw new ReadErrorException(
                $"Failed to read struct. Type={typeof(T).FullName}, StartAddress={startAddress}", ex);
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
                await Task.Delay(retryDelay, cts).ConfigureAwait(false);
            }

            attempt++;
        }

        return false;
    }

    public async Task WriteBitsAsync(ushort startAddress, bool[] values, CancellationToken cts = default) {
        using var execCts = CreateCts(false, cts);
        CancellationToken execToken = execCts?.Token ?? cts;

        try {
            if (values == null || values.Length == 0) {
                throw new WriteErrorException("Values cannot be null or empty.");
            }

            if (values.Length > ModbusTcpConstants.MAX_WRITE_BITS_PER_REQUEST) {
                throw new WriteErrorException($"Too many bits to write: {values.Length}. Allowed range is 1..{ModbusTcpConstants.MAX_WRITE_BITS_PER_REQUEST}.");
            }

            ushort addr = ModbusAddressHelper.NormalizeAddress(startAddress, _config.UseOneBasedAddress);

            ushort tid = NextTransactionId();

            if (values.Length == 1) {
                byte[] request = ModbusTcpFrameBuilder.BuildWriteSingleCoilRequest(tid, _config.UnitId, addr, values[0]);
                byte[] response = await ExecuteAsync(request, execToken).ConfigureAwait(false);
                ModbusTcpFrameParser.ValidateWriteSingleCoilResponse(response, tid, _config.UnitId, addr, values[0]);
                return;
            }

            byte[] req = ModbusTcpFrameBuilder.BuildWriteMultipleCoilsRequest(tid, _config.UnitId, addr, values);
            byte[] resp = await ExecuteAsync(req, execToken).ConfigureAwait(false);
            ModbusTcpFrameParser.ValidateWriteMultipleCoilsResponse(resp, tid, _config.UnitId, addr, (ushort)values.Length);
        } catch (OperationCanceledException ex) {
            if (!cts.IsCancellationRequested && execCts != null && execCts.IsCancellationRequested) {
                throw new TimeoutException($"Modbus TCP write bits timeout. StartAddress={startAddress}, Count={values.Length}.", ex);
            }

            throw;
        } catch (Exception ex) when (ex is not WriteErrorException) {
            throw new WriteErrorException($"Error writing bits. StartAddress={startAddress}, Count={values?.Length ?? 0}.", ex);
        }
    }

    public async Task WriteRegistersAsync(ushort startAddress, ushort[] values, CancellationToken cts = default) {
        using var execCts = CreateCts(false, cts);
        CancellationToken execToken = execCts?.Token ?? cts;

        try {
            if (values == null || values.Length == 0) {
                throw new WriteErrorException("Values cannot be null or empty.");
            }

            if (values.Length > ModbusTcpConstants.MAX_WRITE_REGISTERS_PER_REQUEST) {
                throw new WriteErrorException($"Too many registers to write: {values.Length}. Allowed range is 1..{ModbusTcpConstants.MAX_WRITE_REGISTERS_PER_REQUEST}.");
            }

            ushort addr = ModbusAddressHelper.NormalizeAddress(startAddress, _config.UseOneBasedAddress);

            ushort tid = NextTransactionId();

            if (values.Length == 1) {
                byte[] request = ModbusTcpFrameBuilder.BuildWriteSingleRegisterRequest(tid, _config.UnitId, addr, values[0]);
                byte[] response = await ExecuteAsync(request, execToken).ConfigureAwait(false);
                ModbusTcpFrameParser.ValidateWriteSingleRegisterResponse(response, tid, _config.UnitId, addr, values[0]);
                return;
            }

            byte[] req = ModbusTcpFrameBuilder.BuildWriteMultipleRegistersRequest(tid, _config.UnitId, addr, values);
            byte[] resp = await ExecuteAsync(req, execToken).ConfigureAwait(false);
            ModbusTcpFrameParser.ValidateWriteMultipleRegistersResponse(resp, tid, _config.UnitId, addr, (ushort)values.Length);
        } catch (OperationCanceledException ex) {
            if (!cts.IsCancellationRequested && execCts != null && execCts.IsCancellationRequested) {
                throw new TimeoutException($"Modbus TCP write registers timeout. StartAddress={startAddress}, Count={values.Length}.", ex);
            }

            throw;
        } catch (Exception ex) when (ex is not WriteErrorException) {
            throw new WriteErrorException($"Error writing registers. StartAddress={startAddress}, Count={values?.Length ?? 0}.", ex);
        }
    }

    public async Task WriteStructAsync<T>(ushort startAddress, T value, CancellationToken cts = default) where T : struct {
        try {
            byte[] bytes = StructBinaryHelper.StructToBytes(value);
            ushort[] registers = ModbusRegisterBinaryHelper.LittleEndianBytesToRegisters(
                bytes, _config.ByteOrder, _config.WordOrder);

            if (registers.Length == 0) {
                throw new WriteErrorException($"Struct serialized to empty registers. Type={typeof(T).FullName}.");
            }

            if (registers.Length > ModbusTcpConstants.MAX_WRITE_REGISTERS_PER_REQUEST) {
                throw new WriteErrorException($"Struct too large to write in one request. Type={typeof(T).FullName}, RegisterCount={registers.Length}.");
            }

            await WriteRegistersAsync(
                startAddress,
                registers,
                cts: cts
            ).ConfigureAwait(false);
        } catch (OperationCanceledException) {
            throw;
        } catch (TimeoutException) {
            throw;
        } catch (Exception ex) when (ex is not WriteErrorException) {
            throw new WriteErrorException($"Failed to write struct. Type={typeof(T).FullName}, StartAddress={startAddress}", ex);
        }
    }

    #region 私有方法

    private bool IsSocketAlive() {
        try {
            var socket = _client?.Client;
            if (socket == null || !socket.Connected) {
                return false;
            }

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

            _client = new TcpClient();
            await _client.ConnectAsync(_config.Ip, _config.Port, cts).ConfigureAwait(false);
            _client.Client.NoDelay = true;
            _stream = _client.GetStream();
        } catch (OperationCanceledException) {
            InvalidateConnection();
            throw;
        } catch (Exception) {
            InvalidateConnection();
            throw;
        } finally {
            _ = _connectLock.Release();
        }
    }

    private async Task<byte[]> ExecuteAsync(byte[] requestFrame, CancellationToken cts) {
        if (_stream == null) {
            throw new InvalidOperationException("Network stream is not initialized.");
        }

        await _streamLock.WaitAsync(cts).ConfigureAwait(false);
        try {
            await _stream.WriteAsync(requestFrame, cts).ConfigureAwait(false);
            return await ReadModbusTcpFrameAsync(_stream, cts).ConfigureAwait(false);
        } finally {
            _ = _streamLock.Release();
        }
    }

    private static async Task<byte[]> ReadModbusTcpFrameAsync(NetworkStream stream, CancellationToken cts) {
        byte[] header = new byte[7];
        await ReadExactAsync(stream, header, 0, header.Length, cts).ConfigureAwait(false);

        ushort length = (ushort)((header[4] << 8) | header[5]);
        if (length < 2) {
            throw new ReadErrorException($"Invalid MBAP length: {length}.");
        }

        int remaining = length - 1;
        byte[] pdu = ArrayPool<byte>.Shared.Rent(remaining);
        try {
            await ReadExactAsync(stream, pdu, 0, remaining, cts).ConfigureAwait(false);

            byte[] frame = new byte[7 + remaining];
            Buffer.BlockCopy(header, 0, frame, 0, 7);
            Buffer.BlockCopy(pdu, 0, frame, 7, remaining);
            return frame;
        } finally {
            ArrayPool<byte>.Shared.Return(pdu);
        }
    }

    private static async Task ReadExactAsync(NetworkStream stream, byte[] buffer, int offset, int count, CancellationToken cts) {
        int readTotal = 0;
        while (readTotal < count) {
            int read = await stream.ReadAsync(buffer.AsMemory(offset + readTotal, count - readTotal), cts).ConfigureAwait(false);
            if (read <= 0) {
                throw new ReadErrorException("Connection closed while reading Modbus TCP frame.");
            }

            readTotal += read;
        }
    }

    private void DoDisconnect() {
        InvalidateConnection();
    }

    private ushort NextTransactionId() {
        unchecked {
            _transactionId++;
            if (_transactionId == 0) {
                _transactionId = 1;
            }

            return _transactionId;
        }
    }

    private CancellationTokenSource? CreateCts(bool isRead, CancellationToken cts) {
        int timeoutMs = isRead ? _config.ReadTimeout : _config.WriteTimeout;
        if (timeoutMs <= 0) {
            return null;
        }

        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts);
        linkedCts.CancelAfter(timeoutMs);
        return linkedCts;
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

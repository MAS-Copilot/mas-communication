// =============================================================================
// Professional Automation Equipment Manufacturer.
// Website:       https://www.mas-automation.com/
//
// Copyright (c) MAS(厦门威光) Corporation. All rights reserved.
// =============================================================================

using S7.Net;
using S7.Net.Types;

namespace MAS.Communication.S7Protocol;

// TODO: 不依赖外部库实现 S7 协议的通信功能
internal class S7ProtocolClient: IS7Protocol {
    private readonly IS7CommunicationConfig _config;

    #region 私有字段

    private bool _disposed;
    private readonly Plc _plc;

    #endregion

    public event EventHandler? Disposed;

    public CommProtocol ProtocolType => CommProtocol.S7;

    public ICommunicationConfig Configuration => _config;

    public S7ProtocolClient(IS7CommunicationConfig config) {
        _config = config;
        _plc = new(
            S7ProtocolHelper.ParseCpuType(_config.Type),
            _config.Ip,
            _config.Rack,
            _config.Slot);
    }

    public bool CheckConnection() {
        return _plc.IsConnected;
    }

    public async Task ConnectAsync(CancellationToken cts = default) {
        try {
            await _plc.OpenAsync(cts).ConfigureAwait(false);
        } catch (OperationCanceledException) {
            throw;
        } catch (Exception ex) {
            throw new ConnectionException($"{_config.GetInstanceKey()}: connection failed: {ex.Message}", ex);
        }
    }

    public void Disconnect() {
        _plc.Close();
    }

    public void Dispose() {
        if (_disposed) {
            return;
        }

        _plc.Close();
        _disposed = true;

        Disposed?.Invoke(this, EventArgs.Empty);
    }

    public async Task<bool> ProbeConnectionAsync(CancellationToken cts = default) {
        try {
            if (_plc.IsConnected) {
                return true;
            }

            await _plc.OpenAsync(cts).ConfigureAwait(false);
            return true;
        } catch (OperationCanceledException) {
            throw;
        } catch {
            return false;
        } finally {
            _plc.Close();
        }
    }

    public async Task<object?> ReadAsync(DataType dataType, int db, int startByteAdr, VarType varType, int varCount, CancellationToken cts = default) {
        try {
            return await _plc.ReadAsync(
                dataType,
                db,
                startByteAdr,
                varType,
                varCount,
                cancellationToken: cts).ConfigureAwait(false);
        } catch (OperationCanceledException) {
            throw;
        } catch (Exception ex) {
            throw new ReadErrorException(
                $"{_config.GetInstanceKey()}: read failed. DataType={dataType}, Db={db}, Start={startByteAdr}, VarType={varType}, Count={varCount}.", ex);
        }
    }

    public async Task<byte[]> ReadBytesAsync(DataType dataType, int db, int startByteAdr, int count, CancellationToken cts = default) {
        try {
            byte[] bytes = await _plc.ReadBytesAsync(dataType, db, startByteAdr, count, cts).ConfigureAwait(false);
            return bytes;
        } catch (OperationCanceledException) {
            throw;
        } catch (Exception ex) {
            throw new ReadErrorException(
                $"{_config.GetInstanceKey()}: byte read failed. DataType={dataType}, Db={db}, Start={startByteAdr}, Count={count}.", ex);
        }
    }

    public async Task ReadClassAsync(object sourceClass, int db, int startByteAdr = 0, CancellationToken cts = default) {
        try {
            _ = await _plc.ReadClassAsync(sourceClass, db, startByteAdr, cts).ConfigureAwait(false);
        } catch (OperationCanceledException) {
            throw;
        } catch (Exception ex) {
            throw new ReadErrorException(
                $"{_config.GetInstanceKey()}: class read failed. ClassType={sourceClass.GetType().Name}, Db={db}, Start={startByteAdr}.", ex);
        }
    }

    public async Task ReadMultipleVarsAsync(List<DataItem> dataItems, CancellationToken cts = default) {
        try {
            _ = await _plc.ReadMultipleVarsAsync(dataItems, cts).ConfigureAwait(false);
        } catch (OperationCanceledException) {
            throw;
        } catch (Exception ex) {
            throw new ReadErrorException(
                $"{_config.GetInstanceKey()}: multiple vars read failed. ItemCount={dataItems.Count}.", ex);
        }
    }

    public async Task<T?> ReadStructAsync<T>(int db, int startByteAdr = 0, CancellationToken cts = default) where T : struct {
        try {
            object? result = await _plc.ReadStructAsync(typeof(T), db, startByteAdr, cts).ConfigureAwait(false);
            if (result != null) {
                return (T)result;
            }

            return null;
        } catch (OperationCanceledException) {
            throw;
        } catch (Exception ex) {
            throw new ReadErrorException(
                $"{_config.GetInstanceKey()}: struct read failed. StructType={typeof(T).Name}, Db={db}, Start={startByteAdr}.", ex);
        }
    }

    public async Task<object?> ReadStructAsync(Type structType, int db, int startByteAdr = 0, CancellationToken cts = default) {
        try {
            object? result = await _plc.ReadStructAsync(structType, db, startByteAdr, cts).ConfigureAwait(false);
            if (result != null) {
                return result;
            }

            return null;
        } catch (OperationCanceledException) {
            throw;
        } catch (Exception ex) {
            throw new ReadErrorException(
                $"{_config.GetInstanceKey()}: struct read failed. StructType={structType.Name}, Db={db}, Start={startByteAdr}.", ex);
        }
    }

    public async Task<bool> TryReconnectAsync(int maxAttempts, int retryDelay = 1000, CancellationToken cts = default) {
        int attempt = 0;
        while (attempt < maxAttempts) {
            try {
                await _plc.OpenAsync(cts).ConfigureAwait(false);
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

    public async Task WriteAsync(DataType dataType, int db, int startByteAdr, object value, CancellationToken cts = default) {
        try {
            await _plc.WriteAsync(dataType, db, startByteAdr, value, cancellationToken: cts).ConfigureAwait(false);
        } catch (OperationCanceledException) {
            throw;
        } catch (Exception ex) {
            throw new WriteErrorException(
                $"{_config.GetInstanceKey()}: write failed. DataType={dataType}, Db={db}, Start={startByteAdr}, ValueType={value.GetType().Name}.", ex);
        }
    }

    public async Task WriteBytesAsync(DataType dataType, int db, int startByteAdr, byte[] value, CancellationToken cts = default) {
        try {
            await _plc.WriteBytesAsync(dataType, db, startByteAdr, value, cts).ConfigureAwait(false);
        } catch (OperationCanceledException) {
            throw;
        } catch (Exception ex) {
            throw new WriteErrorException(
               $"{_config.GetInstanceKey()}: byte write failed. DataType={dataType}, Db={db}, Start={startByteAdr}, Count={value.Length}.", ex);
        }
    }

    public async Task WriteClassAsync(object classValue, int db, int startByteAdr = 0, CancellationToken cts = default) {
        try {
            await _plc.WriteClassAsync(classValue, db, startByteAdr, cts).ConfigureAwait(false);
        } catch (OperationCanceledException) {
            throw;
        } catch (Exception ex) {
            throw new WriteErrorException(
                $"{_config.GetInstanceKey()}: class write failed. ClassType={classValue.GetType().Name}, Db={db}, Start={startByteAdr}.", ex);
        }
    }

    public async Task WriteMultipleVarsAsync(List<DataItem> dataItems, CancellationToken cts = default) {
        try {
            await _plc.WriteAsync([.. dataItems]).ConfigureAwait(false);
        } catch (OperationCanceledException) {
            throw;
        } catch (Exception ex) {
            throw new WriteErrorException(
                $"{_config.GetInstanceKey()}: multiple vars write failed. ItemCount={dataItems.Count}.", ex);
        }
    }

    public async Task WriteStructAsync<T>(T structValue, int db, int startByteAdr = 0, CancellationToken cts = default) where T : struct {
        try {
            await _plc.WriteStructAsync(structValue, db, startByteAdr, cts).ConfigureAwait(false);
        } catch (OperationCanceledException) {
            throw;
        } catch (Exception ex) {
            throw new WriteErrorException(
                $"{_config.GetInstanceKey()}: struct write failed. StructType={typeof(T).Name}, Db={db}, Start={startByteAdr}.", ex);
        }
    }

    public async Task WriteStructAsync(object structValue, int db, int startByteAdr = 0, CancellationToken cts = default) {
        try {
            await _plc.WriteStructAsync(structValue, db, startByteAdr, cts).ConfigureAwait(false);
        } catch (OperationCanceledException) {
            throw;
        } catch (Exception ex) {
            throw new WriteErrorException(
                $"{_config.GetInstanceKey()}: struct write failed. StructType={structValue.GetType().Name}, Db={db}, Start={startByteAdr}.", ex);
        }
    }
}

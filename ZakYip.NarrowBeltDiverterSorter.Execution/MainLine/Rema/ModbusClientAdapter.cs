using Microsoft.Extensions.Logging;

namespace ZakYip.NarrowBeltDiverterSorter.Execution.MainLine.Rema;

/// <summary>
/// Modbus 客户端适配器
/// 将现有的 IRemaLm1000HTransport 适配为 IModbusClient 接口
/// </summary>
public sealed class ModbusClientAdapter : IModbusClient
{
    private readonly IRemaLm1000HTransport _transport;
    private readonly ILogger<ModbusClientAdapter> _logger;
    private bool _disposed;

    public ModbusClientAdapter(
        IRemaLm1000HTransport transport,
        ILogger<ModbusClientAdapter> logger)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public bool IsConnected => true; // 假定传输层已连接

    /// <inheritdoc/>
    public Task<OperationResult> ConnectAsync(CancellationToken cancellationToken = default)
    {
        // 传输层不需要显式连接
        return Task.FromResult(OperationResult.Success());
    }

    /// <inheritdoc/>
    public Task<OperationResult> DisconnectAsync()
    {
        // 传输层不需要显式断开
        return Task.FromResult(OperationResult.Success());
    }

    /// <inheritdoc/>
    public async Task<OperationResult<ushort[]>> ReadHoldingRegistersAsync(
        byte slaveAddress, 
        ushort startAddress, 
        ushort count, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var values = await _transport.ReadRegistersAsync(startAddress, count, cancellationToken);
            return OperationResult<ushort[]>.Success(values);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "读取保持寄存器失败：从站={SlaveAddress}, 地址=0x{Address:X4}, 数量={Count}", 
                slaveAddress, startAddress, count);
            return OperationResult<ushort[]>.Failure($"读取寄存器失败：{ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<OperationResult> WriteSingleRegisterAsync(
        byte slaveAddress, 
        ushort registerAddress, 
        ushort value, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _transport.WriteRegisterAsync(registerAddress, value, cancellationToken);
            return OperationResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "写入单个寄存器失败：从站={SlaveAddress}, 地址=0x{Address:X4}, 值={Value}", 
                slaveAddress, registerAddress, value);
            return OperationResult.Failure($"写入寄存器失败：{ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<OperationResult> WriteMultipleRegistersAsync(
        byte slaveAddress, 
        ushort startAddress, 
        ushort[] values, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _transport.WriteRegistersAsync(startAddress, values, cancellationToken);
            return OperationResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "写入多个寄存器失败：从站={SlaveAddress}, 地址=0x{Address:X4}, 数量={Count}", 
                slaveAddress, startAddress, values.Length);
            return OperationResult.Failure($"写入寄存器失败：{ex.Message}", ex);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
    }
}

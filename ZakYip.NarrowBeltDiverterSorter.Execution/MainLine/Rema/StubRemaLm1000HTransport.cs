using Microsoft.Extensions.Logging;

namespace ZakYip.NarrowBeltDiverterSorter.Execution.MainLine.Rema;

/// <summary>
/// 雷马 LM1000H 传输层桩实现
/// 用于开发和测试，不实际与硬件通讯
/// </summary>
public sealed class StubRemaLm1000HTransport : IRemaLm1000HTransport
{
    private readonly ILogger<StubRemaLm1000HTransport> _logger;
    private readonly Dictionary<ushort, ushort> _registers = new();
    private readonly object _lock = new();
    
    /// <summary>
    /// 模拟读取失败（用于测试）
    /// </summary>
    public bool SimulateReadFailure { get; set; }

    public StubRemaLm1000HTransport(ILogger<StubRemaLm1000HTransport> logger)
    {
        _logger = logger;
        InitializeDefaultRegisters();
    }

    /// <inheritdoc/>
    public Task WriteRegisterAsync(ushort address, ushort value, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _registers[address] = value;
            _logger.LogTrace("写入寄存器 0x{Address:X4} = {Value}", address, value);
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<ushort> ReadRegisterAsync(ushort address, CancellationToken cancellationToken = default)
    {
        if (SimulateReadFailure)
        {
            throw new InvalidOperationException("模拟读取失败");
        }
        
        lock (_lock)
        {
            if (_registers.TryGetValue(address, out var value))
            {
                _logger.LogTrace("读取寄存器 0x{Address:X4} = {Value}", address, value);
                return Task.FromResult(value);
            }
            
            _logger.LogTrace("读取寄存器 0x{Address:X4} = 0 (未初始化)", address);
            return Task.FromResult((ushort)0);
        }
    }

    /// <inheritdoc/>
    public async Task WriteRegistersAsync(ushort startAddress, ushort[] values, CancellationToken cancellationToken = default)
    {
        for (ushort i = 0; i < values.Length; i++)
        {
            await WriteRegisterAsync((ushort)(startAddress + i), values[i], cancellationToken);
        }
    }

    /// <inheritdoc/>
    public async Task<ushort[]> ReadRegistersAsync(ushort startAddress, ushort count, CancellationToken cancellationToken = default)
    {
        var result = new ushort[count];
        for (ushort i = 0; i < count; i++)
        {
            result[i] = await ReadRegisterAsync((ushort)(startAddress + i), cancellationToken);
        }
        return result;
    }

    /// <summary>
    /// 初始化默认寄存器值
    /// </summary>
    private void InitializeDefaultRegisters()
    {
        lock (_lock)
        {
            // P2.06 - 额定电流 (默认 6A)
            _registers[RemaRegisters.P2_06_RatedCurrent] = 600; // 6.00A × 100
            
            // C0.32 - 运行状态 (默认停止)
            _registers[RemaRegisters.C0_32_RunStatus] = RemaScaling.RunStatus_Stopped;
            
            // C0.26 - 编码器反馈频率 (默认 0Hz)
            _registers[RemaRegisters.C0_26_EncoderFrequency] = 0;
            
            // C0.01 - 输出电流 (默认 0A)
            _registers[RemaRegisters.C0_01_OutputCurrent] = 0;
        }
    }
}

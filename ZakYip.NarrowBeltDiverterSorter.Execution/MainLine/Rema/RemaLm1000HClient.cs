using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ZakYip.NarrowBeltDiverterSorter.Execution.MainLine.Rema;

/// <summary>
/// 雷马 LM1000H 变频驱动器客户端实现
/// 封装对 LM1000H 的高级业务操作，所有通讯异常都被捕获并转换为 OperationResult
/// </summary>
public sealed class RemaLm1000HClient : IRemaLm1000HClient
{
    private readonly IModbusClient _modbusClient;
    private readonly RemaLm1000HConnectionOptions _connectionOptions;
    private readonly RemaLm1000HOptions _driveOptions;
    private readonly ILogger<RemaLm1000HClient> _logger;
    private bool _disposed;

    public RemaLm1000HClient(
        IModbusClient modbusClient,
        IOptions<RemaLm1000HConnectionOptions> connectionOptions,
        IOptions<RemaLm1000HOptions> driveOptions,
        ILogger<RemaLm1000HClient> logger)
    {
        _modbusClient = modbusClient ?? throw new ArgumentNullException(nameof(modbusClient));
        _connectionOptions = connectionOptions?.Value ?? throw new ArgumentNullException(nameof(connectionOptions));
        _driveOptions = driveOptions?.Value ?? throw new ArgumentNullException(nameof(driveOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public bool IsConnected => _modbusClient.IsConnected;

    /// <inheritdoc/>
    public async Task<OperationResult> ConnectAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("连接到雷马 LM1000H - 端口: {Port}, 波特率: {BaudRate}, 从站: {SlaveAddress}",
            _connectionOptions.PortName, _connectionOptions.BaudRate, _connectionOptions.SlaveAddress);

        return await _modbusClient.ConnectAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<OperationResult> DisconnectAsync()
    {
        _logger.LogInformation("断开雷马 LM1000H 连接");
        return await _modbusClient.DisconnectAsync();
    }

    /// <inheritdoc/>
    public async Task<OperationResult> StartForwardAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("启动电机 - 正转运行");

        var result = await _modbusClient.WriteSingleRegisterAsync(
            _connectionOptions.SlaveAddress,
            RemaRegisters.ControlWord,
            (ushort)RemaScaling.ControlCmd_Forward,
            cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogError("启动电机失败：{Error}", result.ErrorMessage);
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<OperationResult> StopDecelerateAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("停止电机 - 减速停机");

        var result = await _modbusClient.WriteSingleRegisterAsync(
            _connectionOptions.SlaveAddress,
            RemaRegisters.ControlWord,
            (ushort)RemaScaling.ControlCmd_Decelerate,
            cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogError("停止电机失败：{Error}", result.ErrorMessage);
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<OperationResult> SetTargetFrequencyAsync(decimal targetHz, CancellationToken cancellationToken = default)
    {
        var registerValue = ConvertHzToRegisterValue(targetHz);
        
        _logger.LogDebug("设置目标频率：{TargetHz:F2} Hz (寄存器值: {RegisterValue})", targetHz, registerValue);

        var result = await _modbusClient.WriteSingleRegisterAsync(
            _connectionOptions.SlaveAddress,
            RemaRegisters.P0_07_LimitFrequency,
            registerValue,
            cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogError("设置目标频率失败：{Error}", result.ErrorMessage);
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<OperationResult> SetTargetSpeedAsync(decimal targetMmps, CancellationToken cancellationToken = default)
    {
        var targetHz = ConvertMmpsToHz(targetMmps);
        
        _logger.LogDebug("设置目标线速：{TargetMmps} mm/s → {TargetHz:F2} Hz", targetMmps, targetHz);

        return await SetTargetFrequencyAsync(targetHz, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<OperationResult<decimal>> ReadCurrentFrequencyAsync(CancellationToken cancellationToken = default)
    {
        var result = await _modbusClient.ReadHoldingRegistersAsync(
            _connectionOptions.SlaveAddress,
            RemaRegisters.C0_26_EncoderFrequency,
            1,
            cancellationToken);

        if (!result.IsSuccess || result.Value == null)
        {
            _logger.LogError("读取当前频率失败：{Error}", result.ErrorMessage);
            return OperationResult<decimal>.Failure(result.ErrorMessage ?? "读取失败", result.Exception);
        }

        var hz = ConvertRegisterValueToHz(result.Value[0]);
        _logger.LogTrace("读取当前频率：{Hz:F2} Hz (寄存器值: {RegisterValue})", hz, result.Value[0]);

        return OperationResult<decimal>.Success(hz);
    }

    /// <inheritdoc/>
    public async Task<OperationResult<decimal>> ReadCurrentSpeedAsync(CancellationToken cancellationToken = default)
    {
        var freqResult = await ReadCurrentFrequencyAsync(cancellationToken);

        if (!freqResult.IsSuccess)
        {
            return OperationResult<decimal>.Failure(freqResult.ErrorMessage ?? "读取失败", freqResult.Exception);
        }

        var mmps = ConvertHzToMmps(freqResult.Value);
        _logger.LogTrace("读取当前线速：{Mmps} mm/s ({Hz:F2} Hz)", mmps, freqResult.Value);

        return OperationResult<decimal>.Success(mmps);
    }

    /// <inheritdoc/>
    public async Task<OperationResult> SetTorqueLimitAsync(int torqueLimit, CancellationToken cancellationToken = default)
    {
        var clampedTorque = Math.Clamp(torqueLimit, 0, RemaScaling.TorqueMaxAbsolute);
        
        _logger.LogDebug("设置扭矩上限：{TorqueLimit} (原始值: {OriginalValue})", clampedTorque, torqueLimit);

        var result = await _modbusClient.WriteSingleRegisterAsync(
            _connectionOptions.SlaveAddress,
            RemaRegisters.P3_10_TorqueRef,
            (ushort)clampedTorque,
            cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogError("设置扭矩上限失败：{Error}", result.ErrorMessage);
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<OperationResult<decimal>> ReadOutputCurrentAsync(CancellationToken cancellationToken = default)
    {
        var result = await _modbusClient.ReadHoldingRegistersAsync(
            _connectionOptions.SlaveAddress,
            RemaRegisters.C0_01_OutputCurrent,
            1,
            cancellationToken);

        if (!result.IsSuccess || result.Value == null)
        {
            _logger.LogError("读取输出电流失败：{Error}", result.ErrorMessage);
            return OperationResult<decimal>.Failure(result.ErrorMessage ?? "读取失败", result.Exception);
        }

        // 电流值通常是寄存器值的 0.01 倍
        var current = result.Value[0] * 0.01m;
        _logger.LogTrace("读取输出电流：{Current:F2} A (寄存器值: {RegisterValue})", current, result.Value[0]);

        return OperationResult<decimal>.Success(current);
    }

    /// <inheritdoc/>
    public async Task<OperationResult<int>> ReadRunStatusAsync(CancellationToken cancellationToken = default)
    {
        var result = await _modbusClient.ReadHoldingRegistersAsync(
            _connectionOptions.SlaveAddress,
            RemaRegisters.C0_32_RunStatus,
            1,
            cancellationToken);

        if (!result.IsSuccess || result.Value == null)
        {
            _logger.LogError("读取运行状态失败：{Error}", result.ErrorMessage);
            return OperationResult<int>.Failure(result.ErrorMessage ?? "读取失败", result.Exception);
        }

        var status = result.Value[0];
        _logger.LogTrace("读取运行状态：{Status} ({StatusName})", status, GetRunStatusName(status));

        return OperationResult<int>.Success(status);
    }

    /// <inheritdoc/>
    public async Task<OperationResult> InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("初始化雷马 LM1000H 参数");

        // P0.01 - 运行命令源选择 (RS485 通讯 = 2)
        var result = await _modbusClient.WriteSingleRegisterAsync(
            _connectionOptions.SlaveAddress,
            RemaRegisters.P0_01_RunCmdSource,
            2,
            cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogError("初始化失败 - 设置运行命令源：{Error}", result.ErrorMessage);
            return result;
        }

        // P0.07 - 限速频率
        var limitRegisterValue = ConvertHzToRegisterValue(_driveOptions.LimitHz);
        result = await _modbusClient.WriteSingleRegisterAsync(
            _connectionOptions.SlaveAddress,
            RemaRegisters.P0_07_LimitFrequency,
            limitRegisterValue,
            cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogError("初始化失败 - 设置限速频率：{Error}", result.ErrorMessage);
            return result;
        }

        // P3.10 - 转矩给定值上限
        result = await _modbusClient.WriteSingleRegisterAsync(
            _connectionOptions.SlaveAddress,
            RemaRegisters.P3_10_TorqueRef,
            (ushort)Math.Min(_driveOptions.TorqueMax, RemaScaling.TorqueMaxAbsolute),
            cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogError("初始化失败 - 设置转矩上限：{Error}", result.ErrorMessage);
            return result;
        }

        // 可选：P7.07 - 面板显示位
        if (_driveOptions.PanelBits.HasValue)
        {
            result = await _modbusClient.WriteSingleRegisterAsync(
                _connectionOptions.SlaveAddress,
                RemaRegisters.P7_07_PanelDisplayBits,
                (ushort)_driveOptions.PanelBits.Value,
                cancellationToken);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("初始化警告 - 设置面板显示位失败：{Error}", result.ErrorMessage);
                // 继续执行，不影响主要功能
            }
        }

        // 可选：P6.02 - 继电器定义
        if (_driveOptions.RelayDefine.HasValue)
        {
            result = await _modbusClient.WriteSingleRegisterAsync(
                _connectionOptions.SlaveAddress,
                RemaRegisters.P6_02_RelayDefine,
                (ushort)_driveOptions.RelayDefine.Value,
                cancellationToken);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("初始化警告 - 设置继电器定义失败：{Error}", result.ErrorMessage);
                // 继续执行，不影响主要功能
            }
        }

        _logger.LogInformation("雷马 LM1000H 参数初始化完成 - 限速：{LimitHz:F2} Hz，最大扭矩：{TorqueMax}",
            _driveOptions.LimitHz, _driveOptions.TorqueMax);

        return OperationResult.Success();
    }

    /// <summary>
    /// 转换 mm/s 到 Hz
    /// </summary>
    private static decimal ConvertMmpsToHz(decimal mmps)
    {
        return mmps * RemaScaling.MmpsToHz;
    }

    /// <summary>
    /// 转换 Hz 到 mm/s
    /// </summary>
    private static decimal ConvertHzToMmps(decimal hz)
    {
        return hz * RemaScaling.HzToMmps;
    }

    /// <summary>
    /// 转换 Hz 到寄存器值
    /// </summary>
    private static ushort ConvertHzToRegisterValue(decimal hz)
    {
        var registerValue = (int)Math.Round(hz / RemaScaling.P005_HzPerCount);
        return (ushort)Math.Clamp(registerValue, 0, ushort.MaxValue);
    }

    /// <summary>
    /// 转换寄存器值到 Hz
    /// </summary>
    private static decimal ConvertRegisterValueToHz(ushort registerValue)
    {
        return registerValue * RemaScaling.C026_HzPerCount;
    }

    /// <summary>
    /// 获取运行状态名称
    /// </summary>
    private static string GetRunStatusName(int status)
    {
        return status switch
        {
            RemaScaling.RunStatus_Forward => "正转",
            RemaScaling.RunStatus_Reverse => "反转",
            RemaScaling.RunStatus_Stopped => "停止",
            RemaScaling.RunStatus_Tuning => "调谐",
            RemaScaling.RunStatus_Fault => "故障",
            _ => $"未知({status})"
        };
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _modbusClient?.Dispose();
    }
}

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.MainLine;

namespace ZakYip.NarrowBeltDiverterSorter.Execution.Mainline;

/// <summary>
/// 主线控制服务实现
/// 实现基于PID算法的速度控制
/// </summary>
public class MainLineControlService : IMainLineControlService
{
    private readonly ILogger<MainLineControlService> _logger;
    private readonly IMainLineDrivePort _drivePort;
    private readonly IMainLineFeedbackPort _feedbackPort;
    private readonly MainLineControlOptions _options;

    private decimal _targetSpeedMmps;
    private bool _isRunning;
    private decimal _integralError;
    private decimal _previousError;
    private decimal _lastTargetSpeedMmps = decimal.MinValue;
    private readonly object _lock = new();

    public MainLineControlService(
        ILogger<MainLineControlService> logger,
        IMainLineDrivePort drivePort,
        IMainLineFeedbackPort feedbackPort,
        IOptions<MainLineControlOptions> options)
    {
        _logger = logger;
        _drivePort = drivePort;
        _feedbackPort = feedbackPort;
        _options = options.Value;
        _targetSpeedMmps = _options.TargetSpeedMmps;
    }

    /// <inheritdoc/>
    public bool IsRunning
    {
        get
        {
            lock (_lock)
            {
                return _isRunning;
            }
        }
    }

    /// <inheritdoc/>
    public void SetTargetSpeed(decimal targetSpeedMmps)
    {
        lock (_lock)
        {
            // 如果目标速度未变化，直接返回，避免重复写命令
            if (targetSpeedMmps == _lastTargetSpeedMmps)
            {
                return;
            }

            _targetSpeedMmps = targetSpeedMmps;
            _lastTargetSpeedMmps = targetSpeedMmps;
            _logger.LogInformation("目标速度已更新为 {TargetSpeed} mm/s", targetSpeedMmps);
        }
    }

    /// <inheritdoc/>
    public decimal GetTargetSpeed()
    {
        lock (_lock)
        {
            return _targetSpeedMmps;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> StartAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_isRunning)
            {
                _logger.LogWarning("控制服务已在运行中");
                return false;
            }

            _isRunning = true;
            _integralError = 0m;
            _previousError = 0m;
        }

        var result = await _drivePort.StartAsync(cancellationToken);
        if (result)
        {
            _logger.LogInformation("主线控制已启动，目标速度 {TargetSpeed} mm/s", _targetSpeedMmps);
        }
        else
        {
            lock (_lock)
            {
                _isRunning = false;
            }
            _logger.LogError("主线启动失败");
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<bool> StopAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (!_isRunning)
            {
                _logger.LogWarning("控制服务未在运行");
                return false;
            }

            _isRunning = false;
        }

        var result = await _drivePort.StopAsync(cancellationToken);
        if (result)
        {
            _logger.LogInformation("主线控制已停止");
        }
        else
        {
            _logger.LogError("主线停止失败");
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<bool> ExecuteControlLoopAsync(CancellationToken cancellationToken = default)
    {
        if (!IsRunning)
        {
            return false;
        }

        try
        {
            // 读取当前速度
            var currentSpeed = (decimal)_feedbackPort.GetCurrentSpeed();
            
            // 检查故障
            var faultCode = _feedbackPort.GetFaultCode();
            if (faultCode.HasValue)
            {
                _logger.LogError("主线驱动器故障，故障码: {FaultCode}", faultCode.Value);
                lock (_lock)
                {
                    _isRunning = false;
                }
                return false;
            }

            // 计算误差
            var error = _targetSpeedMmps - currentSpeed;

            // PID 控制计算
            var output = CalculatePidOutput(error, _options.LoopPeriod);

            // 限幅
            output = Math.Clamp(output, _options.MinOutputMmps, _options.MaxOutputMmps);

            // 发送到驱动器
            var success = await _drivePort.SetTargetSpeedAsync((double)output, cancellationToken);

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行控制循环时发生异常");
            return false;
        }
    }

    /// <summary>
    /// 计算PID输出
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private decimal CalculatePidOutput(decimal error, TimeSpan loopPeriod)
    {
        lock (_lock)
        {
            var dt = (decimal)loopPeriod.TotalSeconds;

            // 比例项
            var proportional = _options.ProportionalGain * error;

            // 积分项（带限幅）
            _integralError += error * dt;
            _integralError = Math.Clamp(_integralError, -_options.IntegralLimit, _options.IntegralLimit);
            var integral = _options.IntegralGain * _integralError;

            // 微分项
            var derivative = _options.DerivativeGain * (error - _previousError) / dt;
            _previousError = error;

            // PID输出 = 目标速度 + 调节量
            return _targetSpeedMmps + proportional + integral + derivative;
        }
    }

    /// <summary>
    /// mm/s 转换为 Hz
    /// </summary>
    /// <param name="speedMmps">速度（mm/s）</param>
    /// <param name="mmPerRotation">每转毫米数</param>
    /// <returns>频率（Hz）</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static decimal ConvertMmpsToHz(decimal speedMmps, decimal mmPerRotation)
    {
        return speedMmps / mmPerRotation;
    }

    /// <summary>
    /// Hz 转换为 mm/s
    /// </summary>
    /// <param name="frequencyHz">频率（Hz）</param>
    /// <param name="mmPerRotation">每转毫米数</param>
    /// <returns>速度（mm/s）</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static decimal ConvertHzToMmps(decimal frequencyHz, decimal mmPerRotation)
    {
        return frequencyHz * mmPerRotation;
    }
}

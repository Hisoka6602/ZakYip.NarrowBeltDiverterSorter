using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ZakYip.NarrowBeltDiverterSorter.Execution.MainLine.Rema;

/// <summary>
/// 雷马 LM1000H 主线驱动实现
/// 只负责主线速度控制（启动/停止/目标速度/实时速度），不控制格口 IO、发信器、小车吐件
/// </summary>
public sealed class RemaLm1000HMainLineDrive : IMainLineDrive, IDisposable
{
    private readonly ILogger<RemaLm1000HMainLineDrive> _logger;
    private readonly RemaLm1000HOptions _options;
    private readonly IRemaLm1000HTransport _transport;
    
    private decimal _targetSpeedMmps;
    private decimal _currentSpeedMmps;
    private bool _isSpeedStable;
    private DateTime _stableStartTime;
    private DateTime _unstableStartTime;
    private bool _wasStable;
    private bool _wasUnstable;
    
    private readonly Timer _controlLoopTimer;
    private readonly object _lock = new();
    private bool _isRunning;
    private bool _disposed;

    public RemaLm1000HMainLineDrive(
        ILogger<RemaLm1000HMainLineDrive> logger,
        IOptions<RemaLm1000HOptions> options,
        IRemaLm1000HTransport transport)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        
        _targetSpeedMmps = 0m;
        _currentSpeedMmps = 0m;
        _isSpeedStable = false;
        _stableStartTime = DateTime.MinValue;
        _unstableStartTime = DateTime.MinValue;
        _wasStable = false;
        _wasUnstable = false;
        _isRunning = false;
        
        // 创建控制循环定时器
        _controlLoopTimer = new Timer(
            ControlLoopCallback,
            null,
            Timeout.InfiniteTimeSpan,
            Timeout.InfiniteTimeSpan);
    }

    /// <inheritdoc/>
    public async Task SetTargetSpeedAsync(decimal targetSpeedMmps, CancellationToken cancellationToken = default)
    {
        // 限制目标速度在允许范围内
        var clampedSpeed = Math.Clamp(targetSpeedMmps, _options.MinMmps, _options.MaxMmps);
        
        lock (_lock)
        {
            _targetSpeedMmps = clampedSpeed;
        }
        
        // 转换为 Hz
        var targetHz = ConvertMmpsToHz(clampedSpeed);
        
        _logger.LogInformation("设置主线目标速度：{TargetMmps} mm/s (对应 {TargetHz:F2} Hz)", 
            clampedSpeed, targetHz);
        
        // 写入 P0.07 限速频率寄存器
        var registerValue = ConvertHzToRegisterValue(targetHz);
        await _transport.WriteRegisterAsync(
            RemaRegisters.P0_07_LimitFrequency, 
            registerValue, 
            cancellationToken);
    }

    /// <inheritdoc/>
    public decimal CurrentSpeedMmps
    {
        get
        {
            lock (_lock)
            {
                return _currentSpeedMmps;
            }
        }
    }

    /// <inheritdoc/>
    public decimal TargetSpeedMmps
    {
        get
        {
            lock (_lock)
            {
                return _targetSpeedMmps;
            }
        }
    }

    /// <inheritdoc/>
    public bool IsSpeedStable
    {
        get
        {
            lock (_lock)
            {
                return _isSpeedStable;
            }
        }
    }

    /// <summary>
    /// 启动主线驱动
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("启动雷马 LM1000H 主线驱动");
        
        // 初始化 Rema 寄存器
        await InitializeRemaAsync(cancellationToken);
        
        lock (_lock)
        {
            _isRunning = true;
        }
        
        // 启动控制循环定时器
        _controlLoopTimer.Change(_options.LoopPeriod, _options.LoopPeriod);
        
        _logger.LogInformation("主线驱动已启动，控制周期：{LoopPeriod} ms", 
            _options.LoopPeriod.TotalMilliseconds);
    }

    /// <summary>
    /// 停止主线驱动
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("停止雷马 LM1000H 主线驱动");
        
        lock (_lock)
        {
            _isRunning = false;
        }
        
        // 停止控制循环定时器
        _controlLoopTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        
        // 设置安全速度 (0 Hz)
        await _transport.WriteRegisterAsync(
            RemaRegisters.P0_07_LimitFrequency, 
            0, 
            cancellationToken);
        
        // 发送停机命令
        await _transport.WriteRegisterAsync(
            RemaRegisters.ControlWord, 
            RemaScaling.ControlCmd_Decelerate, 
            cancellationToken);
        
        _logger.LogInformation("主线驱动已停止，已设置安全速度 0 Hz");
    }

    /// <summary>
    /// 初始化 Rema 寄存器
    /// </summary>
    private async Task InitializeRemaAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("初始化雷马 LM1000H 寄存器");
        
        // P0.01 - 运行命令源选择 (RS485 通讯)
        await _transport.WriteRegisterAsync(
            RemaRegisters.P0_01_RunCmdSource, 
            2, 
            cancellationToken);
        
        // P0.07 - 限速频率
        var limitRegisterValue = ConvertHzToRegisterValue(_options.LimitHz);
        await _transport.WriteRegisterAsync(
            RemaRegisters.P0_07_LimitFrequency, 
            limitRegisterValue, 
            cancellationToken);
        
        // P3.10 - 转矩给定值上限
        await _transport.WriteRegisterAsync(
            RemaRegisters.P3_10_TorqueRef, 
            (ushort)Math.Min(_options.TorqueMax, RemaScaling.TorqueMaxAbsolute), 
            cancellationToken);
        
        // 如果配置了面板显示位，写入 P7.07
        if (_options.PanelBits.HasValue)
        {
            await _transport.WriteRegisterAsync(
                RemaRegisters.P7_07_PanelDisplayBits, 
                (ushort)_options.PanelBits.Value, 
                cancellationToken);
        }
        
        // 如果配置了继电器定义，写入 P6.02
        if (_options.RelayDefine.HasValue)
        {
            await _transport.WriteRegisterAsync(
                RemaRegisters.P6_02_RelayDefine, 
                (ushort)_options.RelayDefine.Value, 
                cancellationToken);
        }
        
        // 发送正转运行命令
        await _transport.WriteRegisterAsync(
            RemaRegisters.ControlWord, 
            RemaScaling.ControlCmd_Forward, 
            cancellationToken);
        
        _logger.LogInformation("雷马寄存器初始化完成 - 限速：{LimitHz:F2} Hz，最大扭矩：{TorqueMax}", 
            _options.LimitHz, _options.TorqueMax);
    }

    /// <summary>
    /// 控制循环回调
    /// </summary>
    private void ControlLoopCallback(object? state)
    {
        if (!_isRunning || _disposed)
        {
            return;
        }
        
        try
        {
            // 异步执行控制循环
            _ = Task.Run(async () => await ControlLoopAsync());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "控制循环执行异常");
        }
    }

    /// <summary>
    /// 控制循环主逻辑
    /// </summary>
    private async Task ControlLoopAsync()
    {
        try
        {
            // 读取当前速度
            var encoderFreqRegister = await _transport.ReadRegisterAsync(
                RemaRegisters.C0_26_EncoderFrequency);
            
            var currentHz = ConvertRegisterValueToHz(encoderFreqRegister);
            var currentMmps = ConvertHzToMmps(currentHz);
            
            decimal targetMmps;
            lock (_lock)
            {
                _currentSpeedMmps = currentMmps;
                targetMmps = _targetSpeedMmps;
            }
            
            // 更新稳定性状态
            UpdateStabilityState(currentMmps, targetMmps);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "控制循环读取速度异常");
        }
    }

    /// <summary>
    /// 更新速度稳定性状态
    /// </summary>
    private void UpdateStabilityState(decimal currentMmps, decimal targetMmps)
    {
        var error = Math.Abs(currentMmps - targetMmps);
        var now = DateTime.UtcNow;
        
        // 判断是否在稳定死区内
        var isInDeadband = error <= _options.StableDeadbandMmps;
        
        lock (_lock)
        {
            if (isInDeadband && !_wasStable)
            {
                // 刚进入稳定死区
                _stableStartTime = now;
                _wasStable = true;
                _logger.LogDebug("进入稳定死区，当前速度：{CurrentMmps} mm/s，目标：{TargetMmps} mm/s，误差：{Error:F2} mm/s", 
                    currentMmps, targetMmps, error);
            }
            else if (!isInDeadband && _wasStable)
            {
                // 离开稳定死区
                _wasStable = false;
                _stableStartTime = DateTime.MinValue;
                _isSpeedStable = false;
                _logger.LogDebug("离开稳定死区，当前速度：{CurrentMmps} mm/s，目标：{TargetMmps} mm/s，误差：{Error:F2} mm/s", 
                    currentMmps, targetMmps, error);
            }
            
            // 检查稳定持续时间
            if (isInDeadband)
            {
                var stableDuration = now - _stableStartTime;
                if (stableDuration >= _options.StableHold)
                {
                    if (!_isSpeedStable)
                    {
                        _isSpeedStable = true;
                        _logger.LogInformation("主线速度已稳定，当前：{CurrentMmps} mm/s，目标：{TargetMmps} mm/s", 
                            currentMmps, targetMmps);
                    }
                }
            }
            
            // 判断是否不稳定（超过阈值）
            var isUnstable = error >= _options.UnstableThresholdMmps;
            
            if (isUnstable && !_wasUnstable)
            {
                // 刚进入不稳定状态
                _unstableStartTime = now;
                _wasUnstable = true;
            }
            else if (!isUnstable && _wasUnstable)
            {
                // 离开不稳定状态
                _wasUnstable = false;
                _unstableStartTime = DateTime.MinValue;
            }
            
            // 检查不稳定持续时间
            if (isUnstable)
            {
                var unstableDuration = now - _unstableStartTime;
                if (unstableDuration >= _options.UnstableHold)
                {
                    _logger.LogWarning("主线速度长时间不稳定，当前：{CurrentMmps} mm/s，目标：{TargetMmps} mm/s，误差：{Error:F2} mm/s，持续时间：{Duration} 秒", 
                        currentMmps, targetMmps, error, unstableDuration.TotalSeconds);
                    
                    // 重置不稳定开始时间，避免重复告警
                    _unstableStartTime = now;
                }
            }
        }
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
        // 寄存器值 = Hz ÷ 0.01
        var registerValue = (int)Math.Round(hz / RemaScaling.P005_HzPerCount);
        return (ushort)Math.Clamp(registerValue, 0, ushort.MaxValue);
    }

    /// <summary>
    /// 转换寄存器值到 Hz
    /// </summary>
    private static decimal ConvertRegisterValueToHz(ushort registerValue)
    {
        // Hz = 寄存器值 × 0.01
        return registerValue * RemaScaling.C026_HzPerCount;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        
        _disposed = true;
        _controlLoopTimer?.Dispose();
    }
}

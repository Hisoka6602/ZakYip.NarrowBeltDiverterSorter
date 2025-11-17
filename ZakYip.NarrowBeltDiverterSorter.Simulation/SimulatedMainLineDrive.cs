using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.MainLine;
using ZakYip.NarrowBeltDiverterSorter.Execution.MainLine;
using ZakYip.NarrowBeltDiverterSorter.Simulation.Fakes;

namespace ZakYip.NarrowBeltDiverterSorter.Simulation;

/// <summary>
/// 仿真主驱动线控制实现
/// 包装 FakeMainLineDrivePort 和 FakeMainLineFeedbackPort
/// 通过定时器/Loop 模拟速度逼近目标
/// </summary>
public sealed class SimulatedMainLineDrive : IMainLineDrive
{
    private readonly ILogger<SimulatedMainLineDrive> _logger;
    private readonly FakeMainLineDrivePort _drivePort;
    private readonly FakeMainLineFeedbackPort _feedbackPort;
    private readonly IMainLineStabilityProvider _stabilityProvider;
    private decimal _targetSpeedMmps;
    private readonly object _lock = new();
    private bool _isReady;

    public SimulatedMainLineDrive(
        ILogger<SimulatedMainLineDrive> logger,
        FakeMainLineDrivePort drivePort,
        FakeMainLineFeedbackPort feedbackPort,
        IMainLineStabilityProvider stabilityProvider)
    {
        _logger = logger;
        _drivePort = drivePort;
        _feedbackPort = feedbackPort;
        _stabilityProvider = stabilityProvider;
        _targetSpeedMmps = 0m;
        _isReady = false;
    }

    /// <inheritdoc/>
    public async Task SetTargetSpeedAsync(decimal targetSpeedMmps, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _targetSpeedMmps = targetSpeedMmps;
        }
        
        // 调用底层 FakeMainLineDrivePort 设置速度
        await _drivePort.SetTargetSpeedAsync((double)targetSpeedMmps, cancellationToken);
    }

    /// <inheritdoc/>
    public decimal CurrentSpeedMmps
    {
        get
        {
            // 从 FakeMainLineFeedbackPort 获取当前实际速度
            return (decimal)_feedbackPort.GetCurrentSpeed();
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
            // 从稳定性提供者获取稳定状态
            return _stabilityProvider.IsStable;
        }
    }

    /// <inheritdoc/>
    public Task<decimal> GetCurrentSpeedAsync(CancellationToken cancellationToken = default)
    {
        // 仿真模式直接返回当前模拟速度
        return Task.FromResult(CurrentSpeedMmps);
    }
    
    /// <inheritdoc/>
    public bool IsReady
    {
        get
        {
            lock (_lock)
            {
                return _isReady;
            }
        }
    }
    
    /// <inheritdoc/>
    public Task<bool> InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("初始化仿真主线驱动（仅日志，不发真实命令）");
        
        // 仿真模式：模拟启动流程
        _logger.LogInformation("  步骤 1: 发送停止命令（仿真）");
        _logger.LogInformation("  步骤 2: 读取关键参数（仿真）");
        _logger.LogInformation("    - P0.05 顶频: 50.00 Hz（仿真值）");
        _logger.LogInformation("    - P2.06 电机额定电流: 6.00 A（仿真值）");
        _logger.LogInformation("  步骤 3: 设置限频和限扭矩参数（仿真）");
        _logger.LogInformation("    - P0.07 限速频率: 50.00 Hz");
        _logger.LogInformation("    - P3.10 转矩上限: 1000");
        
        lock (_lock)
        {
            _isReady = true;
        }
        
        _logger.LogInformation("仿真主线驱动初始化完成");
        return Task.FromResult(true);
    }
    
    /// <inheritdoc/>
    public async Task<bool> ShutdownAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("停机仿真主线驱动（仅日志，不发真实命令）");
        
        // 仿真模式：模拟停机流程
        _logger.LogInformation("  步骤 1: 设置目标速度为 0（仿真）");
        await SetTargetSpeedAsync(0m, cancellationToken);
        
        _logger.LogInformation("  步骤 2: 等待主线速度降到阈值以下（仿真）");
        // 仿真模式下，速度会快速降到0，无需真正等待
        await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
        
        var currentSpeed = CurrentSpeedMmps;
        _logger.LogInformation("    - 当前速度: {CurrentSpeed:F1} mm/s", currentSpeed);
        
        _logger.LogInformation("  步骤 3: 发送停机命令（仿真）");
        
        lock (_lock)
        {
            _isReady = false;
        }
        
        _logger.LogInformation("仿真主线驱动已安全停机");
        return true;
    }
}

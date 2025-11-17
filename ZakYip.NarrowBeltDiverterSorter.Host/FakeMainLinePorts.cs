using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.MainLine;
using ZakYip.NarrowBeltDiverterSorter.Execution.MainLine;

namespace ZakYip.NarrowBeltDiverterSorter.Host;

/// <summary>
/// 仿真主线驱动端口
/// 用于模拟主线驱动行为
/// </summary>
internal sealed class FakeMainLineDrivePort : IMainLineDrivePort
{
    private double _targetSpeed;
    private bool _isRunning;
    private readonly object _lock = new();

    public Task<bool> StartAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _isRunning = true;
        }
        return Task.FromResult(true);
    }

    public Task<bool> StopAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _isRunning = false;
            _targetSpeed = 0;
        }
        return Task.FromResult(true);
    }

    public Task<bool> EmergencyStopAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _isRunning = false;
            _targetSpeed = 0;
        }
        return Task.FromResult(true);
    }

    public Task<bool> SetTargetSpeedAsync(double targetSpeedMmps, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _targetSpeed = targetSpeedMmps;
        }
        return Task.FromResult(true);
    }

    public double GetTargetSpeed()
    {
        lock (_lock)
        {
            return _targetSpeed;
        }
    }

    public bool IsRunning()
    {
        lock (_lock)
        {
            return _isRunning;
        }
    }
}

/// <summary>
/// 仿真主线反馈端口
/// 用于模拟主线反馈信号
/// </summary>
internal sealed class FakeMainLineFeedbackPort : IMainLineFeedbackPort
{
    private readonly FakeMainLineDrivePort _drivePort;
    private double _currentSpeed;
    private readonly Timer _updateTimer;
    private readonly object _lock = new();

    public FakeMainLineFeedbackPort(FakeMainLineDrivePort drivePort)
    {
        _drivePort = drivePort;
        _currentSpeed = 0;
        
        // 创建定时器模拟速度变化（每50ms更新一次）
        _updateTimer = new Timer(UpdateSpeed, null, TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(50));
    }

    private void UpdateSpeed(object? state)
    {
        if (!_drivePort.IsRunning())
        {
            lock (_lock)
            {
                _currentSpeed = 0;
            }
            return;
        }

        var targetSpeed = _drivePort.GetTargetSpeed();
        
        lock (_lock)
        {
            // 模拟速度渐变：每次更新接近目标速度
            var difference = targetSpeed - _currentSpeed;
            var maxChange = 50.0; // 每50ms最多变化50 mm/s
            
            if (Math.Abs(difference) <= maxChange)
            {
                _currentSpeed = targetSpeed;
            }
            else
            {
                _currentSpeed += Math.Sign(difference) * maxChange;
            }
        }
    }

    public double GetCurrentSpeed()
    {
        lock (_lock)
        {
            return _currentSpeed;
        }
    }

    public MainLineStatus GetCurrentStatus()
    {
        return _drivePort.IsRunning() ? MainLineStatus.Running : MainLineStatus.Stopped;
    }

    public int? GetFaultCode()
    {
        // 仿真环境下没有故障
        return null;
    }
}

/// <summary>
/// 仿真主线驱动实现
/// 包装 FakeMainLineDrivePort 和 FakeMainLineFeedbackPort
/// </summary>
internal sealed class SimulatedMainLineDrive : IMainLineDrive
{
    private readonly FakeMainLineDrivePort _drivePort;
    private readonly FakeMainLineFeedbackPort _feedbackPort;
    private readonly IMainLineStabilityProvider _stabilityProvider;
    private decimal _targetSpeedMmps;
    private readonly object _lock = new();
    private bool _isReady;

    public SimulatedMainLineDrive(
        FakeMainLineDrivePort drivePort,
        FakeMainLineFeedbackPort feedbackPort,
        IMainLineStabilityProvider stabilityProvider)
    {
        _drivePort = drivePort;
        _feedbackPort = feedbackPort;
        _stabilityProvider = stabilityProvider;
        _targetSpeedMmps = 0m;
        _isReady = false;
    }

    public async Task SetTargetSpeedAsync(decimal targetSpeedMmps, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _targetSpeedMmps = targetSpeedMmps;
        }
        
        await _drivePort.SetTargetSpeedAsync((double)targetSpeedMmps, cancellationToken);
    }

    public decimal CurrentSpeedMmps
    {
        get
        {
            return (decimal)_feedbackPort.GetCurrentSpeed();
        }
    }

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

    public bool IsSpeedStable
    {
        get
        {
            return _stabilityProvider.IsStable;
        }
    }

    public Task<decimal> GetCurrentSpeedAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(CurrentSpeedMmps);
    }
    
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
    
    public Task<bool> InitializeAsync(CancellationToken cancellationToken = default)
    {
        // Fake implementation - always succeeds
        lock (_lock)
        {
            _isReady = true;
        }
        return Task.FromResult(true);
    }
    
    public Task<bool> ShutdownAsync(CancellationToken cancellationToken = default)
    {
        // Fake implementation - always succeeds
        lock (_lock)
        {
            _isReady = false;
        }
        return Task.FromResult(true);
    }
}

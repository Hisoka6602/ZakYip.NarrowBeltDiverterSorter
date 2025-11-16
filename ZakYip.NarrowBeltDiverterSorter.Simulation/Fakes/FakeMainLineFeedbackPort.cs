using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;

namespace ZakYip.NarrowBeltDiverterSorter.Simulation.Fakes;

/// <summary>
/// 模拟主驱动线反馈端口
/// 增加一阶惯性，当前速度逐步逼近目标速度
/// </summary>
public class FakeMainLineFeedbackPort : IMainLineFeedbackPort
{
    private readonly FakeMainLineDrivePort _drivePort;
    private double _currentSpeed;
    private DateTimeOffset _lastUpdateTime;
    private readonly object _lock = new();
    
    // 加速度：每秒改变的速度 (mm/s²)
    private const double AccelerationMmpsPerSecond = 2000.0; // 2 m/s²

    public FakeMainLineFeedbackPort(FakeMainLineDrivePort drivePort)
    {
        _drivePort = drivePort;
        _currentSpeed = 0;
        _lastUpdateTime = DateTimeOffset.UtcNow;
    }

    public double GetCurrentSpeed()
    {
        lock (_lock)
        {
            UpdateCurrentSpeed();
            return _drivePort.IsRunning ? _currentSpeed : 0;
        }
    }

    public MainLineStatus GetCurrentStatus()
    {
        return _drivePort.IsRunning ? MainLineStatus.Running : MainLineStatus.Stopped;
    }

    public int? GetFaultCode()
    {
        return null;
    }

    /// <summary>
    /// 更新当前速度，使其逐步逼近目标速度
    /// </summary>
    private void UpdateCurrentSpeed()
    {
        var now = DateTimeOffset.UtcNow;
        var deltaTime = (now - _lastUpdateTime).TotalSeconds;
        _lastUpdateTime = now;

        if (!_drivePort.IsRunning)
        {
            _currentSpeed = 0;
            return;
        }

        var targetSpeed = _drivePort.TargetSpeed;
        var speedDiff = targetSpeed - _currentSpeed;

        if (Math.Abs(speedDiff) < 0.1)
        {
            // 已经足够接近目标速度
            _currentSpeed = targetSpeed;
        }
        else
        {
            // 根据加速度和时间差计算速度变化量
            var maxChange = AccelerationMmpsPerSecond * deltaTime;
            var actualChange = Math.Clamp(speedDiff, -maxChange, maxChange);
            _currentSpeed += actualChange;
        }
    }
}

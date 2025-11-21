using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Enums;

namespace ZakYip.NarrowBeltDiverterSorter.Simulation.Fakes;

/// <summary>
/// 模拟主驱动线反馈端口
/// 增加一阶惯性，当前速度逐步逼近目标速度
/// 支持不稳定速度模式用于测试
/// </summary>
public class FakeMainLineFeedbackPort : IMainLineFeedbackPort
{
    private readonly FakeMainLineDrivePort _drivePort;
    private double _currentSpeed;
    private DateTimeOffset _lastUpdateTime;
    private readonly object _lock = new();
    
    // 加速度：每秒改变的速度 (mm/s²)
    private const double AccelerationMmpsPerSecond = 2000.0; // 2 m/s²
    
    // 不稳定速度模式参数
    private bool _unstableMode;
    private double _oscillationAmplitude; // 波动幅度 (mm/s)
    private double _oscillationFrequency; // 波动频率 (Hz)
    private DateTimeOffset _unstableModeStartTime;

    public FakeMainLineFeedbackPort(FakeMainLineDrivePort drivePort)
    {
        _drivePort = drivePort;
        _currentSpeed = 0;
        _lastUpdateTime = DateTimeOffset.Now;
        _unstableMode = false;
        _oscillationAmplitude = 0;
        _oscillationFrequency = 0;
    }
    
    /// <summary>
    /// 启用不稳定速度模式，模拟速度大幅波动
    /// </summary>
    /// <param name="amplitude">波动幅度（mm/s），例如 500 表示 ±500 mm/s</param>
    /// <param name="frequency">波动频率（Hz），例如 0.5 表示每2秒一个周期</param>
    public void EnableUnstableMode(double amplitude, double frequency)
    {
        lock (_lock)
        {
            _unstableMode = true;
            _oscillationAmplitude = amplitude;
            _oscillationFrequency = frequency;
            _unstableModeStartTime = DateTimeOffset.Now;
        }
    }
    
    /// <summary>
    /// 禁用不稳定速度模式
    /// </summary>
    public void DisableUnstableMode()
    {
        lock (_lock)
        {
            _unstableMode = false;
        }
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
    /// 在不稳定模式下，添加正弦波动
    /// </summary>
    private void UpdateCurrentSpeed()
    {
        var now = DateTimeOffset.Now;
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
        
        // 在不稳定模式下添加速度波动
        if (_unstableMode)
        {
            var elapsedTime = (now - _unstableModeStartTime).TotalSeconds;
            var oscillation = _oscillationAmplitude * Math.Sin(2 * Math.PI * _oscillationFrequency * elapsedTime);
            _currentSpeed += oscillation;
            
            // 确保速度不为负
            _currentSpeed = Math.Max(0, _currentSpeed);
        }
    }
}

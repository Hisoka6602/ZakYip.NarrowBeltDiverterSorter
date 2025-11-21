using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Enums;

namespace ZakYip.NarrowBeltDiverterSorter.Execution.Vendors.Simulated;

/// <summary>
/// 仿真主线驱动端口 (用于仿真和测试)
/// </summary>
public sealed class FakeMainLineDrivePort : IMainLineDrivePort
{
    private double _targetSpeed;
    private bool _isRunning;

    public double TargetSpeed => _targetSpeed;
    public bool IsRunning => _isRunning;

    public Task<bool> SetTargetSpeedAsync(double speedMmPerSec, CancellationToken cancellationToken = default)
    {
        _targetSpeed = speedMmPerSec;
        Console.WriteLine($"[主驱] 设置目标线速: {speedMmPerSec:F2} mm/s");
        return Task.FromResult(true);
    }

    public Task<bool> StartAsync(CancellationToken cancellationToken = default)
    {
        _isRunning = true;
        Console.WriteLine($"[主驱] 主线已启动");
        return Task.FromResult(true);
    }

    public Task<bool> StopAsync(CancellationToken cancellationToken = default)
    {
        _isRunning = false;
        Console.WriteLine($"[主驱] 主线已停止");
        return Task.FromResult(true);
    }

    public Task<bool> EmergencyStopAsync(CancellationToken cancellationToken = default)
    {
        _isRunning = false;
        _targetSpeed = 0;
        Console.WriteLine($"[主驱] 主线急停");
        return Task.FromResult(true);
    }
}

/// <summary>
/// 仿真主线反馈端口 (用于仿真和测试)
/// </summary>
public sealed class FakeMainLineFeedbackPort : IMainLineFeedbackPort
{
    private readonly FakeMainLineDrivePort _drivePort;
    private double _currentSpeed;

    public FakeMainLineFeedbackPort(FakeMainLineDrivePort drivePort)
    {
        _drivePort = drivePort;
        _currentSpeed = 0;
        
        // 启动后台线程模拟速度变化
        _ = Task.Run(async () =>
        {
            while (true)
            {
                await Task.Delay(100);
                SimulateSpeedChange();
            }
        });
    }

    private void SimulateSpeedChange()
    {
        if (!_drivePort.IsRunning)
        {
            // 停止时逐渐减速
            if (_currentSpeed > 0)
            {
                _currentSpeed = Math.Max(0, _currentSpeed - 50);
            }
            return;
        }

        var targetSpeed = _drivePort.TargetSpeed;
        var diff = targetSpeed - _currentSpeed;
        
        // 模拟逐渐接近目标速度
        if (Math.Abs(diff) < 1)
        {
            _currentSpeed = targetSpeed;
        }
        else
        {
            _currentSpeed += diff * 0.1; // 每次接近10%的差距
        }
    }

    public double GetCurrentSpeed()
    {
        return _currentSpeed;
    }

    public MainLineStatus GetCurrentStatus()
    {
        if (!_drivePort.IsRunning)
            return MainLineStatus.Stopped;
        
        if (Math.Abs(_currentSpeed - _drivePort.TargetSpeed) < 10)
            return MainLineStatus.Running;
        
        return MainLineStatus.Starting;
    }

    public int? GetFaultCode()
    {
        return null; // 仿真模式无故障
    }
}

using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;

namespace ZakYip.NarrowBeltDiverterSorter.Simulation.Fakes;

/// <summary>
/// 模拟主驱动线驱动端口
/// </summary>
public class FakeMainLineDrivePort : IMainLineDrivePort
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
        Console.WriteLine($"[主驱] 主线紧急停止");
        return Task.FromResult(true);
    }
}

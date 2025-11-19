using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;

namespace ZakYip.NarrowBeltDiverterSorter.Simulation.Fakes;

/// <summary>
/// 模拟入口输送线端口
/// </summary>
public class FakeInfeedConveyorPort : IInfeedConveyorPort
{
    private double _currentSpeed;
    private bool _isRunning;

    public double GetCurrentSpeed()
    {
        return _isRunning ? _currentSpeed : 0;
    }

    public Task<bool> SetSpeedAsync(double speedMmPerSec, CancellationToken cancellationToken = default)
    {
        _currentSpeed = speedMmPerSec;
        Console.WriteLine($"[入口输送线] 设置速度: {speedMmPerSec:F2} mm/s");
        return Task.FromResult(true);
    }

    public Task<bool> StartAsync(CancellationToken cancellationToken = default)
    {
        _isRunning = true;
        Console.WriteLine($"[入口输送线] 已启动");
        return Task.FromResult(true);
    }

    public Task<bool> StopAsync(CancellationToken cancellationToken = default)
    {
        _isRunning = false;
        Console.WriteLine($"[入口输送线] 已停止");
        return Task.FromResult(true);
    }
}

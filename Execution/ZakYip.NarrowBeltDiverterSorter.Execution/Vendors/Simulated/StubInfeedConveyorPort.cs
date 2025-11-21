using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;

namespace ZakYip.NarrowBeltDiverterSorter.Execution.Vendors.Simulated;

/// <summary>
/// 入口输送线端口占位实现（用于仿真和测试）
/// TODO: 在生产模式下替换为实际硬件实现
/// </summary>
public class StubInfeedConveyorPort : IInfeedConveyorPort
{
    public double GetCurrentSpeed()
    {
        return 0.0;
    }

    public Task<bool> StartAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public Task<bool> StopAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public Task<bool> SetSpeedAsync(double speedMmPerSec, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }
}

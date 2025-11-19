using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;

namespace ZakYip.NarrowBeltDiverterSorter.Host;

/// <summary>
/// 入口输送线端口占位实现
/// TODO: 替换为实际硬件实现
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

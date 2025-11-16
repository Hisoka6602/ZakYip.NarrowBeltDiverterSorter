using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;

namespace ZakYip.NarrowBeltDiverterSorter.Simulation.Fakes;

/// <summary>
/// 模拟格口发信器端口
/// </summary>
public class FakeChuteTransmitterPort : IChuteTransmitterPort
{
    public Task OpenWindowAsync(ChuteId chuteId, TimeSpan openDuration, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[格口发信器] 格口 {chuteId.Value} 打开窗口 {openDuration.TotalMilliseconds:F0}ms");
        return Task.CompletedTask;
    }

    public Task ForceCloseAsync(ChuteId chuteId, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[格口发信器] 格口 {chuteId.Value} 强制关闭");
        return Task.CompletedTask;
    }
}

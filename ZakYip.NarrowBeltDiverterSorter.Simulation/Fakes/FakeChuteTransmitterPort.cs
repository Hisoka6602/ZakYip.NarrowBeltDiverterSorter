using System.Collections.Concurrent;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;

namespace ZakYip.NarrowBeltDiverterSorter.Simulation.Fakes;

/// <summary>
/// 模拟格口发信器端口（带状态跟踪）
/// </summary>
public class FakeChuteTransmitterPort : IChuteTransmitterPort
{
    private readonly ConcurrentDictionary<long, bool> _chuteStates = new();

    /// <summary>
    /// 获取所有格口的状态（格口ID -> 是否打开）
    /// </summary>
    public IReadOnlyDictionary<long, bool> GetChuteStates() => _chuteStates;

    /// <summary>
    /// 检查是否有格口处于打开状态
    /// </summary>
    public bool HasOpenChutes() => _chuteStates.Any(kvp => kvp.Value);

    /// <summary>
    /// 获取打开的格口数量
    /// </summary>
    public int GetOpenChuteCount() => _chuteStates.Count(kvp => kvp.Value);

    public Task OpenWindowAsync(ChuteId chuteId, TimeSpan openDuration, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[格口发信器] 格口 {chuteId.Value} 打开窗口 {openDuration.TotalMilliseconds:F0}ms");
        _chuteStates[chuteId.Value] = true;
        
        // Schedule auto-close after duration
        _ = Task.Run(async () =>
        {
            await Task.Delay(openDuration, cancellationToken);
            _chuteStates[chuteId.Value] = false;
        }, cancellationToken);
        
        return Task.CompletedTask;
    }

    public Task ForceCloseAsync(ChuteId chuteId, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[格口发信器] 格口 {chuteId.Value} 强制关闭");
        _chuteStates[chuteId.Value] = false;
        return Task.CompletedTask;
    }
}

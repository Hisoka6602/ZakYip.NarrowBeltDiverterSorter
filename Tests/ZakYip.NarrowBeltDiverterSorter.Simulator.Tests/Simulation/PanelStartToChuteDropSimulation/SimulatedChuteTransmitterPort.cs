using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Chutes;

namespace ZakYip.NarrowBeltDiverterSorter.Simulator.Tests.Simulation.PanelStartToChuteDropSimulation;

/// <summary>
/// 仿真格口发信器端口 - 用于跟踪落格事件
/// </summary>
public sealed class SimulatedChuteTransmitterPort : IChuteTransmitterPort
{
    private readonly List<EjectionEvent> _ejectionEvents = new();
    private readonly SimulationClock _clock;

    /// <summary>
    /// 落格事件记录
    /// </summary>
    public sealed record EjectionEvent(
        long ChuteId,
        int Tick,
        TimeSpan Duration);

    public SimulatedChuteTransmitterPort(SimulationClock clock)
    {
        _clock = clock;
    }

    /// <inheritdoc/>
    public Task OpenWindowAsync(ChuteId chuteId, TimeSpan openDuration, CancellationToken cancellationToken = default)
    {
        // 记录落格事件
        _ejectionEvents.Add(new EjectionEvent(
            chuteId.Value,
            _clock.CurrentTick,
            openDuration));

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task ForceCloseAsync(ChuteId chuteId, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public IReadOnlyList<ChuteTransmitterBinding> GetRegisteredBindings()
    {
        return Array.Empty<ChuteTransmitterBinding>();
    }

    /// <summary>
    /// 获取所有落格事件
    /// </summary>
    public IReadOnlyList<EjectionEvent> GetEjectionEvents()
    {
        return _ejectionEvents;
    }

    /// <summary>
    /// 清空落格事件记录
    /// </summary>
    public void ClearEvents()
    {
        _ejectionEvents.Clear();
    }
}

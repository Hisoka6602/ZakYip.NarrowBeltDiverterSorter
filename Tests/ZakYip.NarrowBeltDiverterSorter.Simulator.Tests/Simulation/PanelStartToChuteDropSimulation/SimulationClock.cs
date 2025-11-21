namespace ZakYip.NarrowBeltDiverterSorter.Simulator.Tests.Simulation.PanelStartToChuteDropSimulation;

/// <summary>
/// 仿真时钟 - 提供离散时间步进
/// </summary>
public sealed class SimulationClock
{
    /// <summary>
    /// 当前时刻（毫秒）
    /// </summary>
    public int CurrentTick { get; private set; }

    /// <summary>
    /// 时钟事件：每次时间推进时触发
    /// </summary>
    public event EventHandler<int>? TickAdvanced;

    /// <summary>
    /// 推进时钟
    /// </summary>
    /// <param name="ticks">推进的时间步数（毫秒）</param>
    public void Advance(int ticks = 1)
    {
        CurrentTick += ticks;
        TickAdvanced?.Invoke(this, CurrentTick);
    }

    /// <summary>
    /// 重置时钟
    /// </summary>
    public void Reset()
    {
        CurrentTick = 0;
    }
}

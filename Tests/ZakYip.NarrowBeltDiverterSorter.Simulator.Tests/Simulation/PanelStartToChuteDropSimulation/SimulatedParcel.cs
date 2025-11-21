namespace ZakYip.NarrowBeltDiverterSorter.Simulator.Tests.Simulation.PanelStartToChuteDropSimulation;

/// <summary>
/// 仿真包裹记录
/// </summary>
public sealed record SimulatedParcel
{
    /// <summary>
    /// 包裹ID
    /// </summary>
    public required long PackageId { get; init; }

    /// <summary>
    /// 目标格口ID
    /// </summary>
    public required int TargetChuteId { get; init; }

    /// <summary>
    /// 上料时刻（仿真时钟tick）
    /// </summary>
    public required int FeedingTick { get; init; }
}

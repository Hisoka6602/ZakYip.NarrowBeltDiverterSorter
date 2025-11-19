namespace ZakYip.NarrowBeltDiverterSorter.Core.SelfCheck;

/// <summary>
/// 格口-小车映射自检选项。
/// </summary>
public sealed record ChuteCartMappingSelfCheckOptions
{
    /// <summary>
    /// 自检时使用的循环圈数，例如 5 圈。
    /// </summary>
    public required int LoopCount { get; init; }

    /// <summary>
    /// 允许的小车编号漂移容忍度（单位：辆），通常为 0 或 1。
    /// </summary>
    public required int CartIdTolerance { get; init; }

    /// <summary>
    /// 允许的格口位置误差（单位：毫米）。
    /// 用于对比几何位置时的容差。
    /// </summary>
    public required decimal PositionToleranceMm { get; init; }
}

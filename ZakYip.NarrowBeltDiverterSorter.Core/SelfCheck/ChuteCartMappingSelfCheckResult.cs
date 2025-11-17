namespace ZakYip.NarrowBeltDiverterSorter.Core.SelfCheck;

/// <summary>
/// 格口-小车映射自检结果。
/// </summary>
public sealed record ChuteCartMappingSelfCheckResult
{
    /// <summary>
    /// 格口总数量。
    /// </summary>
    public required int ChuteCount { get; init; }

    /// <summary>
    /// 小车总数量。
    /// </summary>
    public required int CartCount { get; init; }

    /// <summary>
    /// 按格口汇总的检查结果。
    /// </summary>
    public required IReadOnlyList<ChuteCartMappingCheckItem> ChuteItems { get; init; }

    /// <summary>
    /// 是否所有格口均在容差范围内保持正确映射。
    /// </summary>
    public required bool IsAllPassed { get; init; }
}

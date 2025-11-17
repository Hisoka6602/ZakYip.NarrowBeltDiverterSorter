namespace ZakYip.NarrowBeltDiverterSorter.Core.SelfCheck;

/// <summary>
/// 单个格口的映射检查结果。
/// </summary>
public sealed record ChuteCartMappingCheckItem
{
    /// <summary>
    /// 格口编号（逻辑 ChuteId）。
    /// </summary>
    public required int ChuteId { get; init; }

    /// <summary>
    /// 自检中期望的小车编号（根据初始拓扑推算）。
    /// </summary>
    public required int ExpectedCartId { get; init; }

    /// <summary>
    /// 在 N 圈仿真过程中实际观测到的小车编号序列。
    /// </summary>
    public required IReadOnlyList<int> ObservedCartIds { get; init; }

    /// <summary>
    /// 该格口映射是否通过检查（所有观测值均在容差范围内）。
    /// </summary>
    public required bool IsPassed { get; init; }
}

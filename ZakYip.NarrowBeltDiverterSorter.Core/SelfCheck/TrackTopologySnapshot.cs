namespace ZakYip.NarrowBeltDiverterSorter.Core.SelfCheck;

/// <summary>
/// 轨道拓扑快照
/// 用于小车自检时提供配置信息
/// </summary>
public sealed record TrackTopologySnapshot
{
    /// <summary>
    /// 配置的小车数量
    /// </summary>
    public required int CartCount { get; init; }

    /// <summary>
    /// 配置的小车节距（mm）
    /// </summary>
    public required decimal CartSpacingMm { get; init; }

    /// <summary>
    /// 环总长（mm）
    /// </summary>
    public required decimal RingTotalLengthMm { get; init; }
}

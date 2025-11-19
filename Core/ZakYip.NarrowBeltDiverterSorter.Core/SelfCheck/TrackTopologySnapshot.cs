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

    /// <summary>
    /// 格口数量
    /// </summary>
    public required int ChuteCount { get; init; }

    /// <summary>
    /// 格口宽度（mm）
    /// </summary>
    public required decimal ChuteWidthMm { get; init; }

    /// <summary>
    /// 小车宽度（mm）
    /// </summary>
    public required decimal CartWidthMm { get; init; }

    /// <summary>
    /// 主线长度（mm）
    /// 通常计算为：格口宽度 × 格口数量 / 2
    /// </summary>
    public required decimal TrackLengthMm { get; init; }
}

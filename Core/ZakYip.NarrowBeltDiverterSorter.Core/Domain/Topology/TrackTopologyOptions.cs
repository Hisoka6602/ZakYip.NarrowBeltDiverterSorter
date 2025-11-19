namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.Topology;

/// <summary>
/// 格口位置配置
/// </summary>
public record ChutePositionConfig
{
    /// <summary>
    /// 格口ID
    /// </summary>
    public required ChuteId ChuteId { get; init; }

    /// <summary>
    /// 小车偏移量（相对于原点的小车数量）
    /// </summary>
    public required int CartOffsetFromOrigin { get; init; }
}

/// <summary>
/// 轨道拓扑配置选项
/// </summary>
public class TrackTopologyOptions
{
    /// <summary>
    /// 小车数量
    /// </summary>
    public int CartCount { get; set; }

    /// <summary>
    /// 小车节距（mm）
    /// </summary>
    public decimal CartSpacingMm { get; set; }

    /// <summary>
    /// 小车宽度（mm）
    /// </summary>
    public decimal CartWidthMm { get; set; }

    /// <summary>
    /// 格口宽度（mm）
    /// </summary>
    public decimal ChuteWidthMm { get; set; }

    /// <summary>
    /// 格口位置配置列表
    /// </summary>
    public List<ChutePositionConfig> ChutePositions { get; set; } = new();

    /// <summary>
    /// 强排口格口ID（0或null表示无强排口）
    /// </summary>
    public int? ForceEjectChuteId { get; set; }

    /// <summary>
    /// 入口落包点相对于原点的距离（mm）
    /// </summary>
    public decimal InfeedDropPointOffsetMm { get; set; }
}

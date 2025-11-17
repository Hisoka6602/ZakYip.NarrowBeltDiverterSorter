namespace ZakYip.NarrowBeltDiverterSorter.Core.Configuration;

/// <summary>
/// 格口布局配置文件。
/// </summary>
public sealed record ChuteLayoutProfile
{
    /// <summary>
    /// 格口数量，例如 60。
    /// </summary>
    public int ChuteCount { get; init; } = 60;

    /// <summary>
    /// 单个格口宽度（毫米），例如 1000mm。
    /// </summary>
    public decimal ChuteWidthMm { get; init; } = 1000m;

    /// <summary>
    /// 异常口格口 ID，例如最后一个格口。
    /// 若为 null，则使用最后一个格口（ChuteCount）。
    /// </summary>
    public int? ExceptionChuteId { get; init; }

    /// <summary>
    /// 格口中心位置（毫米），支持显式指定各格口位置（用于非等距情况）。
    /// 若为 null，则按等距分布计算。
    /// Key 为格口 ID，Value 为格口中心位置（毫米）。
    /// </summary>
    public Dictionary<int, decimal>? ChutePositions { get; init; }

    /// <summary>
    /// 获取实际异常口 ID。
    /// </summary>
    public int GetExceptionChuteId() => ExceptionChuteId ?? ChuteCount;

    /// <summary>
    /// 创建默认配置。
    /// </summary>
    public static ChuteLayoutProfile CreateDefault()
    {
        return new ChuteLayoutProfile();
    }
}

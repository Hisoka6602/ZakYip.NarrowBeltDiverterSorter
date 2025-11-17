namespace ZakYip.NarrowBeltDiverterSorter.Host.DTOs;

/// <summary>
/// 长跑高负载测试选项 DTO。
/// </summary>
public sealed record LongRunLoadTestOptionsDto
{
    /// <summary>
    /// 目标包裹总数。
    /// </summary>
    public required int TargetParcelCount { get; init; }

    /// <summary>
    /// 包裹创建间隔（毫秒）。
    /// </summary>
    public required int ParcelCreationIntervalMs { get; init; }

    /// <summary>
    /// 格口数量。
    /// </summary>
    public required int ChuteCount { get; init; }

    /// <summary>
    /// 单个格口宽度（毫米）。
    /// </summary>
    public required decimal ChuteWidthMm { get; init; }

    /// <summary>
    /// 主线稳态速度（毫米/秒）。
    /// </summary>
    public required decimal MainLineSpeedMmps { get; init; }

    /// <summary>
    /// 小车宽度（毫米）。
    /// </summary>
    public required decimal CartWidthMm { get; init; }

    /// <summary>
    /// 小车节距（毫米）。
    /// </summary>
    public required decimal CartSpacingMm { get; init; }

    /// <summary>
    /// 小车数量。
    /// </summary>
    public required int CartCount { get; init; }

    /// <summary>
    /// 异常口格口编号。
    /// </summary>
    public required int ExceptionChuteId { get; init; }

    /// <summary>
    /// 包裹长度最小值（毫米）。
    /// </summary>
    public required decimal MinParcelLengthMm { get; init; }

    /// <summary>
    /// 包裹长度最大值（毫米）。
    /// </summary>
    public required decimal MaxParcelLengthMm { get; init; }

    /// <summary>
    /// 当预测无法安全分拣时是否强制改派至异常口。
    /// </summary>
    public required bool ForceToExceptionChuteOnConflict { get; init; }

    /// <summary>
    /// 入口到落车点距离（毫米）。
    /// </summary>
    public required decimal InfeedToDropDistanceMm { get; init; }

    /// <summary>
    /// 入口输送线速度（毫米/秒）。
    /// </summary>
    public required decimal InfeedConveyorSpeedMmps { get; init; }
}

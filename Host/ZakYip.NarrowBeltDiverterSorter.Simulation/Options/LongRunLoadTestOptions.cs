namespace ZakYip.NarrowBeltDiverterSorter.Simulation.Options;

/// <summary>
/// 长时间高负载分拣稳定性仿真选项。
/// </summary>
public sealed record LongRunLoadTestOptions
{
    /// <summary>
    /// 目标包裹总数，例如 1000。
    /// </summary>
    public required int TargetParcelCount { get; init; }

    /// <summary>
    /// 包裹创建间隔（毫秒），例如 300ms。
    /// </summary>
    public required int ParcelCreationIntervalMs { get; init; }

    /// <summary>
    /// 格口数量，例如 60。
    /// </summary>
    public required int ChuteCount { get; init; }

    /// <summary>
    /// 单个格口宽度（毫米），例如 1000mm。
    /// </summary>
    public required decimal ChuteWidthMm { get; init; }

    /// <summary>
    /// 主线稳态速度（毫米/秒），例如 1000mm/s。
    /// </summary>
    public required decimal MainLineSpeedMmps { get; init; }

    /// <summary>
    /// 小车宽度（毫米），例如 200mm。
    /// </summary>
    public required decimal CartWidthMm { get; init; }

    /// <summary>
    /// 小车节距（毫米），例如 500mm。
    /// </summary>
    public required decimal CartSpacingMm { get; init; }

    /// <summary>
    /// 小车数量，例如 60。
    /// </summary>
    public required int CartCount { get; init; }

    /// <summary>
    /// 异常口格口编号，例如 60。
    /// </summary>
    public required int ExceptionChuteId { get; init; }

    /// <summary>
    /// 包裹长度最小值（毫米），例如 200mm。
    /// </summary>
    public required decimal MinParcelLengthMm { get; init; }

    /// <summary>
    /// 包裹长度最大值（毫米），例如 1000mm。
    /// </summary>
    public required decimal MaxParcelLengthMm { get; init; }

    /// <summary>
    /// 当预测无法安全分拣时是否强制改派至异常口。
    /// </summary>
    public required bool ForceToExceptionChuteOnConflict { get; init; }

    /// <summary>
    /// 入口到落车点距离（毫米），例如 2000mm。
    /// </summary>
    public required decimal InfeedToDropDistanceMm { get; init; }

    /// <summary>
    /// 入口输送线速度（毫米/秒），例如 1000mm/s。
    /// </summary>
    public required decimal InfeedConveyorSpeedMmps { get; init; }

    /// <summary>
    /// 创建默认配置。
    /// </summary>
    public static LongRunLoadTestOptions CreateDefault()
    {
        return new LongRunLoadTestOptions
        {
            TargetParcelCount = 1000,
            ParcelCreationIntervalMs = 300,
            ChuteCount = 60,
            ChuteWidthMm = 1000m,
            MainLineSpeedMmps = 1000m,
            CartWidthMm = 200m,
            CartSpacingMm = 500m,
            CartCount = 60,
            ExceptionChuteId = 60,
            MinParcelLengthMm = 200m,
            MaxParcelLengthMm = 1000m,
            ForceToExceptionChuteOnConflict = true,
            InfeedToDropDistanceMm = 2000m,
            InfeedConveyorSpeedMmps = 1000m
        };
    }
}

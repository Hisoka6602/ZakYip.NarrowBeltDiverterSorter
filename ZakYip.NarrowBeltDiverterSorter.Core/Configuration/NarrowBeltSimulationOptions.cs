namespace ZakYip.NarrowBeltDiverterSorter.Core.Configuration;

/// <summary>
/// 窄带分拣仿真场景配置选项。
/// </summary>
public sealed record NarrowBeltSimulationOptions
{
    /// <summary>
    /// 包裹创建间隔（毫秒），默认 300ms。
    /// </summary>
    public int TimeBetweenParcelsMs { get; init; } = 300;

    /// <summary>
    /// 仿真总包裹数，例如 1000。
    /// </summary>
    public int TotalParcels { get; init; } = 1000;

    /// <summary>
    /// 包裹长度最小值（毫米），例如 200mm。
    /// </summary>
    public decimal MinParcelLengthMm { get; init; } = 200m;

    /// <summary>
    /// 包裹长度最大值（毫米），例如 800mm。
    /// </summary>
    public decimal MaxParcelLengthMm { get; init; } = 800m;

    /// <summary>
    /// 随机种子（用于结果回放），若为 null 则使用随机种子。
    /// </summary>
    public int? RandomSeed { get; init; }

    /// <summary>
    /// 包裹生命周期超时时间（秒），默认 60 秒。
    /// 超过此时间未落格的包裹将被判定为失败。
    /// </summary>
    public int ParcelTtlSeconds { get; init; } = 60;

    /// <summary>
    /// 创建默认配置。
    /// </summary>
    public static NarrowBeltSimulationOptions CreateDefault()
    {
        return new NarrowBeltSimulationOptions();
    }
}

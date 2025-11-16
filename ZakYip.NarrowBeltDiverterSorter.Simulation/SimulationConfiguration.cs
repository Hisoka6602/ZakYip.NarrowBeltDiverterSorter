namespace ZakYip.NarrowBeltDiverterSorter.Simulation;

/// <summary>
/// 仿真配置
/// </summary>
public class SimulationConfiguration
{
    /// <summary>
    /// 小车数量
    /// </summary>
    public int NumberOfCarts { get; set; } = 20;

    /// <summary>
    /// 小车节距（mm）
    /// </summary>
    public decimal CartSpacingMm { get; set; } = 500m;

    /// <summary>
    /// 格口数量
    /// </summary>
    public int NumberOfChutes { get; set; } = 10;

    /// <summary>
    /// 强排口ID（0表示无强排口）
    /// </summary>
    public int ForceEjectChuteId { get; set; } = 10;

    /// <summary>
    /// 主线速度（mm/s）
    /// </summary>
    public double MainLineSpeedMmPerSec { get; set; } = 1000.0;

    /// <summary>
    /// 入口输送线速度（mm/s）
    /// </summary>
    public double InfeedConveyorSpeedMmPerSec { get; set; } = 1000.0;

    /// <summary>
    /// 入口到落车点距离（mm）
    /// </summary>
    public decimal InfeedToDropDistanceMm { get; set; } = 2000m;

    /// <summary>
    /// 包裹生成间隔（秒）
    /// </summary>
    public double ParcelGenerationIntervalSeconds { get; set; } = 2.0;

    /// <summary>
    /// 包裹存活时间（秒）- 超过此时间的包裹将被强排
    /// </summary>
    public double ParcelTimeToLiveSeconds { get; set; } = 120.0;

    /// <summary>
    /// 仿真持续时间（秒，0表示无限）
    /// </summary>
    public int SimulationDurationSeconds { get; set; } = 60;

    /// <summary>
    /// E2E 仿真包裹数量（仅在 E2E 模式下使用，0表示无限）
    /// </summary>
    public int ParcelCount { get; set; } = 0;
}

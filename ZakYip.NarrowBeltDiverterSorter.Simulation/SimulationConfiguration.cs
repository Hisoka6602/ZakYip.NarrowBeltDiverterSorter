namespace ZakYip.NarrowBeltDiverterSorter.Simulation;

/// <summary>
/// 分拣模式
/// </summary>
public enum SortingMode
{
    /// <summary>
    /// 正式分拣模式：通过上游 RuleEngine 分配格口
    /// </summary>
    Normal,

    /// <summary>
    /// 指定落格模式：始终路由到固定格口
    /// </summary>
    FixedChute,

    /// <summary>
    /// 循环格口模式：按格口列表循环分配
    /// </summary>
    RoundRobin
}

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
    /// 格口宽度（mm）
    /// </summary>
    public decimal ChuteWidthMm { get; set; } = 500m;

    /// <summary>
    /// 小车宽度（mm）
    /// </summary>
    public decimal CartWidthMm { get; set; } = 200m;

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

    /// <summary>
    /// 分拣模式（仅在仿真中使用）
    /// </summary>
    public SortingMode SortingMode { get; set; } = SortingMode.Normal;

    /// <summary>
    /// 固定格口ID（仅在 FixedChute 模式下使用）
    /// </summary>
    public int? FixedChuteId { get; set; } = null;
    
    /// <summary>
    /// 仿真场景（例如：e2e-speed-unstable 用于测试速度不稳定场景）
    /// 可选值：
    /// - null 或 "e2e-report": 标准E2E报告场景
    /// - "e2e-speed-unstable": 速度不稳定场景
    /// - "ChuteIoHardwareDryRun": 格口IO硬件空跑场景，验证格口开闭逻辑
    /// - "cart-self-check": 小车环自检场景，验证小车数量和节距配置
    /// </summary>
    public string? Scenario { get; set; } = null;
    
    /// <summary>
    /// 速度波动幅度（mm/s），用于不稳定速度场景
    /// </summary>
    public double SpeedOscillationAmplitude { get; set; } = 500.0;
    
    /// <summary>
    /// 速度波动频率（Hz），用于不稳定速度场景
    /// </summary>
    public double SpeedOscillationFrequency { get; set; } = 0.5;
}

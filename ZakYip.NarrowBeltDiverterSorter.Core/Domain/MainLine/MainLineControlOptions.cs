namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.MainLine;

/// <summary>
/// 主线控制选项配置
/// </summary>
public class MainLineControlOptions
{
    /// <summary>
    /// 默认目标速度（mm/s）
    /// </summary>
    public decimal TargetSpeedMmps { get; set; } = 1000m;

    /// <summary>
    /// 控制循环周期
    /// </summary>
    public TimeSpan LoopPeriod { get; set; } = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// PID 比例增益
    /// </summary>
    public decimal ProportionalGain { get; set; } = 1.0m;

    /// <summary>
    /// PID 积分增益
    /// </summary>
    public decimal IntegralGain { get; set; } = 0.1m;

    /// <summary>
    /// PID 微分增益
    /// </summary>
    public decimal DerivativeGain { get; set; } = 0.01m;

    /// <summary>
    /// 稳定判据死区（mm/s）
    /// </summary>
    public decimal StableDeadbandMmps { get; set; } = 10m;

    /// <summary>
    /// 稳定判据保持时间
    /// </summary>
    public TimeSpan StableHold { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// 输出限幅最小值（mm/s）
    /// </summary>
    public decimal MinOutputMmps { get; set; } = 0m;

    /// <summary>
    /// 输出限幅最大值（mm/s）
    /// </summary>
    public decimal MaxOutputMmps { get; set; } = 5000m;

    /// <summary>
    /// 积分限幅
    /// </summary>
    public decimal IntegralLimit { get; set; } = 1000m;
}

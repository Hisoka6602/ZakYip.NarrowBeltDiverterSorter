namespace ZakYip.NarrowBeltDiverterSorter.Core.SelfCheck;

/// <summary>
/// 小车环自检配置选项
/// </summary>
public class CartRingSelfCheckOptions
{
    /// <summary>
    /// 最小采样时长（秒）
    /// 需要收集足够多的样本才能进行准确分析
    /// </summary>
    public double MinSamplingDurationSeconds { get; set; } = 30.0;

    /// <summary>
    /// 节距误差容忍百分比（0.05 表示 5%）
    /// </summary>
    public double PitchTolerancePercent { get; set; } = 0.05;

    /// <summary>
    /// 允许的小车ID漏检率（0.0 表示不允许漏检）
    /// </summary>
    public double AllowedMissDetectionRate { get; set; } = 0.0;

    /// <summary>
    /// 最少需要的完整环数（用于计算采样数量）
    /// </summary>
    public int MinCompleteRings { get; set; } = 2;
}

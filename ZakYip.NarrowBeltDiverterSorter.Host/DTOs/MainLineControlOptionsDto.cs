namespace ZakYip.NarrowBeltDiverterSorter.Host.DTOs;

/// <summary>
/// 主线控制选项 DTO。
/// </summary>
public sealed record MainLineControlOptionsDto
{
    /// <summary>
    /// 目标速度（毫米/秒）。
    /// </summary>
    public required decimal TargetSpeedMmps { get; init; }

    /// <summary>
    /// 控制循环周期（毫秒）。
    /// </summary>
    public required int LoopPeriodMs { get; init; }

    /// <summary>
    /// PID 控制器比例系数。
    /// </summary>
    public required decimal ProportionalGain { get; init; }

    /// <summary>
    /// PID 控制器积分系数。
    /// </summary>
    public required decimal IntegralGain { get; init; }

    /// <summary>
    /// PID 控制器微分系数。
    /// </summary>
    public required decimal DerivativeGain { get; init; }

    /// <summary>
    /// 稳定判据死区（毫米/秒）。
    /// </summary>
    public required decimal StableDeadbandMmps { get; init; }

    /// <summary>
    /// 稳定判据保持时间（秒）。
    /// </summary>
    public required int StableHoldSeconds { get; init; }

    /// <summary>
    /// 输出限幅最小值（毫米/秒）。
    /// </summary>
    public required decimal MinOutputMmps { get; init; }

    /// <summary>
    /// 输出限幅最大值（毫米/秒）。
    /// </summary>
    public required decimal MaxOutputMmps { get; init; }

    /// <summary>
    /// 积分限幅。
    /// </summary>
    public required decimal IntegralLimit { get; init; }
}

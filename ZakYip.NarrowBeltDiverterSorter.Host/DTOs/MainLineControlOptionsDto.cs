namespace ZakYip.NarrowBeltDiverterSorter.Host.DTOs;

/// <summary>
/// 主线控制选项 DTO。
/// </summary>
public sealed record MainLineControlOptionsDto
{
    /// <summary>
    /// 主线最大速度（毫米/秒）。
    /// </summary>
    public required decimal MaxSpeedMmps { get; init; }

    /// <summary>
    /// 主线稳态速度（毫米/秒）。
    /// </summary>
    public required decimal SteadySpeedMmps { get; init; }

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
}

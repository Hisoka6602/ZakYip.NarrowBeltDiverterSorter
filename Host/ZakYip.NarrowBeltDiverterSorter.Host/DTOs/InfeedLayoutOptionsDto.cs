namespace ZakYip.NarrowBeltDiverterSorter.Host.DTOs;

/// <summary>
/// 入口布局选项 DTO。
/// </summary>
public sealed record InfeedLayoutOptionsDto
{
    /// <summary>
    /// 入口IO到主线落车点距离（毫米）。
    /// </summary>
    public required decimal InfeedToMainLineDistanceMm { get; init; }

    /// <summary>
    /// 时间容差（毫秒）。
    /// </summary>
    public required int TimeToleranceMs { get; init; }

    /// <summary>
    /// 以小车数计的偏移校准。
    /// </summary>
    public required int CartOffsetCalibration { get; init; }
}

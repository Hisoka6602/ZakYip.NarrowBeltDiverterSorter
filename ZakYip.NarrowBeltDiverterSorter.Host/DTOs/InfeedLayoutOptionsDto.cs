namespace ZakYip.NarrowBeltDiverterSorter.Host.DTOs;

/// <summary>
/// 入口布局选项 DTO。
/// </summary>
public sealed record InfeedLayoutOptionsDto
{
    /// <summary>
    /// 入口到落车点距离（毫米）。
    /// </summary>
    public required decimal InfeedToDropDistanceMm { get; init; }

    /// <summary>
    /// 入口输送线速度（毫米/秒）。
    /// </summary>
    public required decimal InfeedConveyorSpeedMmps { get; init; }
}

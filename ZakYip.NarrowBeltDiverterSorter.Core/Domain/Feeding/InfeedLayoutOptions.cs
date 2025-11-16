namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.Feeding;

/// <summary>
/// 入口布局配置选项
/// </summary>
public record class InfeedLayoutOptions
{
    /// <summary>
    /// 入口IO到主线落车点距离（毫米）
    /// </summary>
    public required decimal InfeedToMainLineDistanceMm { get; init; }

    /// <summary>
    /// 时间容差（毫秒）
    /// </summary>
    public required int TimeToleranceMs { get; init; }

    /// <summary>
    /// 以小车数计的偏移校准
    /// </summary>
    public required int CartOffsetCalibration { get; init; }

    /// <summary>
    /// 创建默认配置
    /// </summary>
    public static InfeedLayoutOptions CreateDefault()
    {
        return new InfeedLayoutOptions
        {
            InfeedToMainLineDistanceMm = 5000m,
            TimeToleranceMs = 100,
            CartOffsetCalibration = 0
        };
    }
}

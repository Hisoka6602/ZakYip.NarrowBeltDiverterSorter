namespace ZakYip.NarrowBeltDiverterSorter.Core.SelfCheck;

/// <summary>
/// 格口 IO 触发事件载荷（仿真或真实采集）。
/// </summary>
public readonly record struct ChutePassEventArgs
{
    /// <summary>
    /// 格口编号。
    /// </summary>
    public required int ChuteId { get; init; }

    /// <summary>
    /// 当前通过该格口的小车编号。
    /// </summary>
    public required int CartId { get; init; }

    /// <summary>
    /// 触发时间。
    /// </summary>
    public required DateTimeOffset TriggeredAt { get; init; }

    /// <summary>
    /// 当前主线速度（mm/s），用于辅助分析。
    /// </summary>
    public required decimal LineSpeedMmps { get; init; }
}

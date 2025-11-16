namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.Tracking;

/// <summary>
/// 原点传感器边沿事件参数
/// </summary>
public readonly record struct OriginSensorEdgeEventArgs
{
    /// <summary>
    /// 是否为第一个传感器
    /// </summary>
    public required bool IsFirstSensor { get; init; }

    /// <summary>
    /// 是否为上升沿（true=上升沿，false=下降沿）
    /// </summary>
    public required bool IsRisingEdge { get; init; }

    /// <summary>
    /// 时间戳
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }
}

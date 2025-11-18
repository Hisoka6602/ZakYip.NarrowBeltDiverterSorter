namespace ZakYip.NarrowBeltDiverterSorter.Observability.Events;

/// <summary>
/// 传感器触发事件参数（用于事件总线）
/// </summary>
public record class SensorTriggeredEventArgs
{
    /// <summary>
    /// 传感器ID
    /// </summary>
    public required string SensorId { get; init; }

    /// <summary>
    /// 触发时间
    /// </summary>
    public required DateTimeOffset TriggerTime { get; init; }

    /// <summary>
    /// 是否被触发
    /// </summary>
    public required bool IsTriggered { get; init; }

    /// <summary>
    /// 是否上升沿
    /// </summary>
    public bool IsRisingEdge { get; init; }
}

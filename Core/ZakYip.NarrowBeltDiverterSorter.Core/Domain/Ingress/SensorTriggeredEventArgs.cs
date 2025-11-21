namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.Ingress;

/// <summary>
/// 传感器触发事件参数
/// 当IO监控检测到传感器状态变化时发布
/// </summary>
public record class SensorTriggeredEventArgs
{
    /// <summary>
    /// 传感器标识
    /// </summary>
    public required string SensorId { get; init; }

    /// <summary>
    /// 触发时间
    /// </summary>
    public required DateTimeOffset TriggerTime { get; init; }

    /// <summary>
    /// 传感器状态（true=遮挡/触发，false=无遮挡）
    /// </summary>
    public required bool IsTriggered { get; init; }

    /// <summary>
    /// 边沿类型（true=上升沿，false=下降沿）
    /// </summary>
    public required bool IsRisingEdge { get; init; }
}

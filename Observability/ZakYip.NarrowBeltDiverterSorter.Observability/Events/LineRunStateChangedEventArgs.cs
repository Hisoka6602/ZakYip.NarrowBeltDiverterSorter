namespace ZakYip.NarrowBeltDiverterSorter.Observability.Events;

/// <summary>
/// 线体运行状态变化事件参数（可观测性层）
/// </summary>
public record class LineRunStateChangedEventArgs
{
    /// <summary>
    /// 当前线体运行状态（字符串表示）
    /// </summary>
    public required string State { get; init; }

    /// <summary>
    /// 状态变化的详细消息
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// 事件发生时间（本地时间）
    /// </summary>
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.Now;
}

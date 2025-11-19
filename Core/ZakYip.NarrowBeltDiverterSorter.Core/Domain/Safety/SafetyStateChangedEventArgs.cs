namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.Safety;

/// <summary>
/// 安全状态变化事件参数
/// </summary>
public record class SafetyStateChangedEventArgs
{
    /// <summary>
    /// 当前安全状态
    /// </summary>
    public required SafetyState State { get; init; }

    /// <summary>
    /// 安全事件源（例如"EStopPanel1"、"SafetyDoorA"）
    /// </summary>
    public string? Source { get; init; }

    /// <summary>
    /// 状态变化的详细消息
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// 事件发生时间
    /// </summary>
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}

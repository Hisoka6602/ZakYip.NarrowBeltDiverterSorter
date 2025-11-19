namespace ZakYip.NarrowBeltDiverterSorter.Observability.Events;

/// <summary>
/// 安全输入变化事件参数（用于事件总线）
/// </summary>
public record class SafetyInputChangedEventArgs
{
    /// <summary>
    /// 输入源
    /// </summary>
    public required string Source { get; init; }

    /// <summary>
    /// 输入类型
    /// </summary>
    public required string InputType { get; init; }

    /// <summary>
    /// 是否激活/安全
    /// </summary>
    public required bool IsActive { get; init; }

    /// <summary>
    /// 发生时间
    /// </summary>
    public required DateTimeOffset OccurredAt { get; init; }
}

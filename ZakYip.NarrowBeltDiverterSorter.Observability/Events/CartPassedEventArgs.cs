namespace ZakYip.NarrowBeltDiverterSorter.Observability.Events;

/// <summary>
/// 小车通过原点事件参数（用于事件总线）
/// </summary>
public record class CartPassedEventArgs
{
    /// <summary>
    /// 小车ID
    /// </summary>
    public required long CartId { get; init; }

    /// <summary>
    /// 通过时间
    /// </summary>
    public required DateTimeOffset PassedAt { get; init; }
}

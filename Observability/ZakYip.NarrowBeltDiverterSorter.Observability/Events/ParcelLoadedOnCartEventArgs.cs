namespace ZakYip.NarrowBeltDiverterSorter.Observability.Events;

/// <summary>
/// 包裹装载到小车事件参数（用于事件总线）
/// </summary>
public record class ParcelLoadedOnCartEventArgs
{
    /// <summary>
    /// 包裹ID
    /// </summary>
    public required long ParcelId { get; init; }

    /// <summary>
    /// 小车ID
    /// </summary>
    public required long CartId { get; init; }

    /// <summary>
    /// 装载时间
    /// </summary>
    public required DateTimeOffset LoadedAt { get; init; }
}

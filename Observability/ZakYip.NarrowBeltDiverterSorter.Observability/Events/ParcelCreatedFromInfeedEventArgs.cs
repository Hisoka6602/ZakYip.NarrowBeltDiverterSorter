namespace ZakYip.NarrowBeltDiverterSorter.Observability.Events;

/// <summary>
/// 包裹从入口创建事件参数（用于事件总线）
/// </summary>
public record class ParcelCreatedFromInfeedEventArgs
{
    /// <summary>
    /// 包裹ID
    /// </summary>
    public required long ParcelId { get; init; }

    /// <summary>
    /// 条码
    /// </summary>
    public required string Barcode { get; init; }

    /// <summary>
    /// 入口触发时间
    /// </summary>
    public required DateTimeOffset InfeedTriggerTime { get; init; }
}

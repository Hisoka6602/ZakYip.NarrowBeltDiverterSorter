namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.Parcels;

/// <summary>
/// 包裹生命周期时间线事件参数
/// </summary>
public readonly record struct ParcelTimelineEventArgs
{
    /// <summary>
    /// 包裹 ID
    /// </summary>
    public required long ParcelId { get; init; }

    /// <summary>
    /// 事件类型
    /// </summary>
    public required ParcelTimelineEventType EventType { get; init; }

    /// <summary>
    /// 发生时间
    /// </summary>
    public required DateTimeOffset OccurredAt { get; init; }

    /// <summary>
    /// 条码（可选）
    /// </summary>
    public string? Barcode { get; init; }

    /// <summary>
    /// 格口 ID（可选）
    /// </summary>
    public long? ChuteId { get; init; }

    /// <summary>
    /// 小车 ID（可选）
    /// </summary>
    public long? CartId { get; init; }

    /// <summary>
    /// 上游关联 ID（可选）
    /// </summary>
    public string? UpstreamCorrelationId { get; init; }

    /// <summary>
    /// 备注信息（可选）
    /// </summary>
    public string? Note { get; init; }
}

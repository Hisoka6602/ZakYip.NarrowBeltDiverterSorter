namespace ZakYip.NarrowBeltDiverterSorter.Observability.Events;

/// <summary>
/// 包裹生命周期变化事件参数（用于事件总线）
/// </summary>
public record class ParcelLifecycleChangedEventArgs
{
    /// <summary>
    /// 包裹ID
    /// </summary>
    public required long ParcelId { get; init; }

    /// <summary>
    /// 状态
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// 失败原因
    /// </summary>
    public string? FailureReason { get; init; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remarks { get; init; }

    /// <summary>
    /// 发生时间
    /// </summary>
    public required DateTimeOffset OccurredAt { get; init; }
}

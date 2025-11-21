namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.Parcels;

/// <summary>
/// 包裹生命周期状态变化事件参数
/// </summary>
public record class ParcelLifecycleChangedEventArgs
{
    /// <summary>
    /// 包裹ID
    /// </summary>
    public required ParcelId ParcelId { get; init; }

    /// <summary>
    /// 新的生命周期状态
    /// </summary>
    public required ParcelStatus Status { get; init; }

    /// <summary>
    /// 失败原因（如果适用）
    /// </summary>
    public ParcelFailureReason FailureReason { get; init; }

    /// <summary>
    /// 事件发生时间
    /// </summary>
    public DateTimeOffset OccurredAt { get; init; }

    /// <summary>
    /// 可选的附加信息或备注
    /// </summary>
    public string? Remarks { get; init; }
}

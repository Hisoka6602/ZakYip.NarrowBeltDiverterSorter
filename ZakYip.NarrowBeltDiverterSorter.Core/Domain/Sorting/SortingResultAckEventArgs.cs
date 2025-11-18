namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.Sorting;

/// <summary>
/// 分拣结果确认事件参数
/// 用于向规则引擎上报分拣结果时的事件载荷
/// </summary>
public record class SortingResultAckEventArgs
{
    /// <summary>
    /// 包裹ID（毫秒时间戳）
    /// </summary>
    public required long ParcelId { get; init; }

    /// <summary>
    /// 格口编号
    /// </summary>
    public required int ChuteNumber { get; init; }

    /// <summary>
    /// 小车编号
    /// </summary>
    public required int CartNumber { get; init; }

    /// <summary>
    /// 小车总数
    /// </summary>
    public required int CartCount { get; init; }

    /// <summary>
    /// 分拣是否成功
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// 处理耗时（毫秒）
    /// </summary>
    public decimal ProcessingTimeMs { get; init; }

    /// <summary>
    /// 失败原因（如果失败）
    /// </summary>
    public string? FailureReason { get; init; }

    /// <summary>
    /// 确认时间
    /// </summary>
    public DateTimeOffset AckTime { get; init; } = DateTimeOffset.UtcNow;
}

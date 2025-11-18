namespace ZakYip.NarrowBeltDiverterSorter.UpstreamContracts.Models;

/// <summary>
/// 分拣结果消息
/// 用于向规则引擎上报包裹的分拣结果（与 RuleEngine 文档对齐）
/// </summary>
public record class SortingResultMessage
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
    /// 结果上报时间
    /// </summary>
    public DateTimeOffset ReportTime { get; init; } = DateTimeOffset.UtcNow;
}

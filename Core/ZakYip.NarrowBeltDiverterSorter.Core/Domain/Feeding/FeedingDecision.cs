namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.Feeding;

/// <summary>
/// 供包决策结果
/// </summary>
public enum FeedingDecision
{
    /// <summary>
    /// 允许创建包裹
    /// </summary>
    Allow,

    /// <summary>
    /// 建议延迟创建包裹（降速模式）
    /// </summary>
    Delay,

    /// <summary>
    /// 拒绝创建包裹（暂停模式）
    /// </summary>
    Reject
}

/// <summary>
/// 供包决策结果详情
/// </summary>
public record FeedingDecisionResult
{
    /// <summary>
    /// 决策结果
    /// </summary>
    public required FeedingDecision Decision { get; init; }

    /// <summary>
    /// 决策原因
    /// </summary>
    public required string Reason { get; init; }

    /// <summary>
    /// 当前在途包裹数
    /// </summary>
    public int CurrentInFlightCount { get; init; }

    /// <summary>
    /// 当前上游等待数
    /// </summary>
    public int CurrentUpstreamPendingCount { get; init; }

    /// <summary>
    /// 建议的延迟时间（毫秒）
    /// 仅在 Decision 为 Delay 时有效
    /// </summary>
    public int? SuggestedDelayMs { get; init; }
}

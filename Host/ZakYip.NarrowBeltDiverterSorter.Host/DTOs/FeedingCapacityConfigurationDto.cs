namespace ZakYip.NarrowBeltDiverterSorter.Host.DTOs;

/// <summary>
/// 供包容量配置 DTO
/// </summary>
public sealed record FeedingCapacityConfigurationDto
{
    /// <summary>
    /// 主线上允许的最大在途包裹数
    /// </summary>
    public required int MaxInFlightParcels { get; init; }

    /// <summary>
    /// 允许等待上游决策的最大请求数
    /// </summary>
    public required int MaxUpstreamPendingRequests { get; init; }

    /// <summary>
    /// 供包节流模式（None, SlowDown, Pause）
    /// </summary>
    public required string ThrottleMode { get; init; }

    /// <summary>
    /// 降速模式下的延长间隔倍数
    /// </summary>
    public required double SlowDownMultiplier { get; init; }

    /// <summary>
    /// 在途包裹数低于此阈值时恢复正常供包（可选）
    /// </summary>
    public int? RecoveryThreshold { get; init; }

    /// <summary>
    /// 当前在途包裹数（只读统计）
    /// </summary>
    public int? CurrentInFlightParcels { get; init; }

    /// <summary>
    /// 当前上游等待数（只读统计）
    /// </summary>
    public int? CurrentUpstreamPendingRequests { get; init; }

    /// <summary>
    /// 供包节流次数（只读统计）
    /// </summary>
    public long? FeedingThrottledCount { get; init; }

    /// <summary>
    /// 供包暂停次数（只读统计）
    /// </summary>
    public long? FeedingPausedCount { get; init; }
}

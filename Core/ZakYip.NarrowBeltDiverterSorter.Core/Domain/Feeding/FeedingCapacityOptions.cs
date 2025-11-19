namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.Feeding;

/// <summary>
/// 供包容量控制配置
/// 定义在途包裹容量限制和背压策略
/// </summary>
public record FeedingCapacityOptions
{
    /// <summary>
    /// 主线上允许的最大在途包裹数
    /// 超过此值将触发背压策略
    /// </summary>
    public int MaxInFlightParcels { get; init; } = 200;

    /// <summary>
    /// 允许等待上游决策的最大请求数
    /// 防止上游规则引擎过载
    /// </summary>
    public int MaxUpstreamPendingRequests { get; init; } = 10;

    /// <summary>
    /// 供包节流模式
    /// </summary>
    public FeedingThrottleMode ThrottleMode { get; init; } = FeedingThrottleMode.None;

    /// <summary>
    /// 降速模式下的延长间隔倍数
    /// 例如：2.0 表示供包间隔延长为原来的 2 倍
    /// </summary>
    public double SlowDownMultiplier { get; init; } = 2.0;

    /// <summary>
    /// 在途包裹数低于此阈值时恢复正常供包
    /// 默认为 MaxInFlightParcels 的 80%
    /// </summary>
    public int? RecoveryThreshold { get; init; }

    /// <summary>
    /// 获取实际的恢复阈值
    /// 如果未设置 RecoveryThreshold，则返回 MaxInFlightParcels 的 80%
    /// </summary>
    public int GetRecoveryThreshold()
    {
        return RecoveryThreshold ?? (int)(MaxInFlightParcels * 0.8);
    }
}

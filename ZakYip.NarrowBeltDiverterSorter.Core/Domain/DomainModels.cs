namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain;

/// <summary>
/// 包裹快照
/// </summary>
public record class ParcelSnapshot
{
    /// <summary>
    /// 包裹ID
    /// </summary>
    public required ParcelId ParcelId { get; init; }

    /// <summary>
    /// 目标格口ID
    /// </summary>
    public ChuteId? TargetChuteId { get; init; }

    /// <summary>
    /// 绑定的小车ID
    /// </summary>
    public CartId? BoundCartId { get; init; }

    /// <summary>
    /// 路由状态
    /// </summary>
    public ParcelRouteState RouteState { get; init; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// 装载时间
    /// </summary>
    public DateTimeOffset? LoadedAt { get; init; }

    /// <summary>
    /// 分拣完成时间
    /// </summary>
    public DateTimeOffset? SortedAt { get; init; }

    /// <summary>
    /// 分拣结果
    /// </summary>
    public ParcelSortingOutcome? SortingOutcome { get; init; }

    /// <summary>
    /// 实际格口ID（实际落格位置）
    /// </summary>
    public ChuteId? ActualChuteId { get; init; }

    /// <summary>
    /// 丢弃原因（仅当被强排时有效）
    /// </summary>
    public ParcelDiscardReason? DiscardReason { get; init; }

    /// <summary>
    /// 包裹生命周期状态（用于可观测性和统一报告）
    /// </summary>
    public ParcelStatus Status { get; init; }

    /// <summary>
    /// 包裹失败原因（仅当 Status 为 Failed、DivertedToException 或 Expired 时有效）
    /// </summary>
    public ParcelFailureReason FailureReason { get; init; }

    /// <summary>
    /// 计划生成时间
    /// </summary>
    public DateTimeOffset? DivertPlannedAt { get; init; }

    /// <summary>
    /// 落格时间（实际执行分拣动作的时间）
    /// </summary>
    public DateTimeOffset? DivertedAt { get; init; }

    /// <summary>
    /// 完成时间（最终状态确定的时间）
    /// </summary>
    public DateTimeOffset? CompletedAt { get; init; }

    /// <summary>
    /// 预测的小车ID（用于验证小车匹配）
    /// </summary>
    public CartId? PredictedCartId { get; init; }
}

/// <summary>
/// 小车快照
/// </summary>
public record class CartSnapshot
{
    /// <summary>
    /// 小车ID
    /// </summary>
    public required CartId CartId { get; init; }

    /// <summary>
    /// 小车索引
    /// </summary>
    public required CartIndex CartIndex { get; init; }

    /// <summary>
    /// 是否已装载
    /// </summary>
    public bool IsLoaded { get; init; }

    /// <summary>
    /// 当前包裹ID
    /// </summary>
    public ParcelId? CurrentParcelId { get; init; }

    /// <summary>
    /// 上次复位时间
    /// </summary>
    public DateTimeOffset LastResetAt { get; init; }
}

/// <summary>
/// 格口配置
/// </summary>
public record class ChuteConfig
{
    /// <summary>
    /// 格口ID
    /// </summary>
    public required ChuteId ChuteId { get; init; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; init; }

    /// <summary>
    /// 是否强制弹出
    /// </summary>
    public bool IsForceEject { get; init; }

    /// <summary>
    /// 相对原点小车偏移
    /// </summary>
    public int CartOffsetFromOrigin { get; init; }

    /// <summary>
    /// 最大打开持续时间
    /// </summary>
    public TimeSpan MaxOpenDuration { get; init; }
}

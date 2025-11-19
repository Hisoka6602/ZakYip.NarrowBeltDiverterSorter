namespace ZakYip.NarrowBeltDiverterSorter.Observability.LiveView;

/// <summary>
/// 主线速度快照
/// </summary>
public record class LineSpeedSnapshot
{
    /// <summary>
    /// 实际速度 (mm/s)
    /// </summary>
    public decimal ActualMmps { get; init; }

    /// <summary>
    /// 目标速度 (mm/s)
    /// </summary>
    public decimal TargetMmps { get; init; }

    /// <summary>
    /// 速度状态
    /// </summary>
    public LineSpeedStatus Status { get; init; }

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTimeOffset LastUpdatedAt { get; init; }
}

/// <summary>
/// 原点小车快照
/// </summary>
public record class OriginCartSnapshot
{
    /// <summary>
    /// 小车ID（null表示无小车）
    /// </summary>
    public long? CartId { get; init; }

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTimeOffset LastUpdatedAt { get; init; }
}

/// <summary>
/// 格口小车快照
/// </summary>
public record class ChuteCartSnapshot
{
    /// <summary>
    /// 格口ID到小车ID的映射
    /// </summary>
    public IReadOnlyDictionary<long, long?> Mapping { get; init; } = new Dictionary<long, long?>();

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTimeOffset LastUpdatedAt { get; init; }
}

/// <summary>
/// 包裹摘要
/// </summary>
public record class ParcelSummary
{
    /// <summary>
    /// 包裹ID
    /// </summary>
    public long ParcelId { get; init; }

    /// <summary>
    /// 条码
    /// </summary>
    public string Barcode { get; init; } = string.Empty;

    /// <summary>
    /// 重量 (kg)
    /// </summary>
    public decimal? WeightKg { get; init; }

    /// <summary>
    /// 体积 (立方毫米)
    /// </summary>
    public decimal? VolumeCubicMm { get; init; }

    /// <summary>
    /// 目标格口ID
    /// </summary>
    public long? TargetChuteId { get; init; }

    /// <summary>
    /// 实际格口ID
    /// </summary>
    public long? ActualChuteId { get; init; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// 落格时间
    /// </summary>
    public DateTimeOffset? DivertedAt { get; init; }
}

/// <summary>
/// 设备状态快照
/// </summary>
public record class DeviceStatusSnapshot
{
    /// <summary>
    /// 设备状态
    /// </summary>
    public DeviceStatus Status { get; init; }

    /// <summary>
    /// 状态消息
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTimeOffset LastUpdatedAt { get; init; }
}

/// <summary>
/// 小车布局快照
/// </summary>
public record class CartLayoutSnapshot
{
    /// <summary>
    /// 小车位置列表
    /// </summary>
    public IReadOnlyList<CartPositionSnapshot> CartPositions { get; init; } = Array.Empty<CartPositionSnapshot>();

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTimeOffset LastUpdatedAt { get; init; }
}

/// <summary>
/// 线体运行状态快照
/// </summary>
public record class LineRunStateSnapshot
{
    /// <summary>
    /// 当前线体运行状态
    /// </summary>
    public string State { get; init; } = string.Empty;

    /// <summary>
    /// 状态消息
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTimeOffset LastUpdatedAt { get; init; }
}

/// <summary>
/// 安全状态快照
/// </summary>
public record class SafetyStateSnapshot
{
    /// <summary>
    /// 当前安全状态
    /// </summary>
    public string State { get; init; } = string.Empty;

    /// <summary>
    /// 安全事件源
    /// </summary>
    public string? Source { get; init; }

    /// <summary>
    /// 状态消息
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTimeOffset LastUpdatedAt { get; init; }
}

/// <summary>
/// 供包容量快照
/// </summary>
public record class FeedingCapacitySnapshot
{
    /// <summary>
    /// 当前在途包裹数
    /// </summary>
    public int CurrentInFlightParcels { get; init; }

    /// <summary>
    /// 最大在途包裹数限制
    /// </summary>
    public int MaxInFlightParcels { get; init; }

    /// <summary>
    /// 当前等待上游决策的包裹数
    /// </summary>
    public int CurrentUpstreamPendingRequests { get; init; }

    /// <summary>
    /// 最大上游等待数限制
    /// </summary>
    public int MaxUpstreamPendingRequests { get; init; }

    /// <summary>
    /// 供包节流次数（累计）
    /// </summary>
    public long FeedingThrottledCount { get; init; }

    /// <summary>
    /// 供包暂停次数（累计）
    /// </summary>
    public long FeedingPausedCount { get; init; }

    /// <summary>
    /// 当前节流模式
    /// </summary>
    public string ThrottleMode { get; init; } = string.Empty;

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTimeOffset LastUpdatedAt { get; init; }
}

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

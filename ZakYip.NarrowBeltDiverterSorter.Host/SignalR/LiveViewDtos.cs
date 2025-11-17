namespace ZakYip.NarrowBeltDiverterSorter.Host.SignalR;

/// <summary>
/// 主线速度 DTO
/// </summary>
public record class LineSpeedDto
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
    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTimeOffset LastUpdatedAt { get; init; }
}

/// <summary>
/// 包裹 DTO
/// </summary>
public record class ParcelDto
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
/// 设备状态 DTO
/// </summary>
public record class DeviceStatusDto
{
    /// <summary>
    /// 设备状态
    /// </summary>
    public string Status { get; init; } = string.Empty;

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
/// 小车位置 DTO
/// </summary>
public record class CartPositionDto
{
    /// <summary>
    /// 小车ID
    /// </summary>
    public long CartId { get; init; }

    /// <summary>
    /// 小车索引
    /// </summary>
    public int CartIndex { get; init; }

    /// <summary>
    /// 线性位置 (mm)
    /// </summary>
    public decimal? LinearPositionMm { get; init; }

    /// <summary>
    /// 当前格口ID
    /// </summary>
    public long? CurrentChuteId { get; init; }
}

/// <summary>
/// 小车布局 DTO
/// </summary>
public record class CartLayoutDto
{
    /// <summary>
    /// 小车位置列表
    /// </summary>
    public List<CartPositionDto> CartPositions { get; init; } = new();

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTimeOffset LastUpdatedAt { get; init; }
}

/// <summary>
/// 格口小车映射 DTO
/// </summary>
public record class ChuteCartDto
{
    /// <summary>
    /// 格口ID
    /// </summary>
    public long ChuteId { get; init; }

    /// <summary>
    /// 小车ID
    /// </summary>
    public long? CartId { get; init; }
}

/// <summary>
/// 原点小车 DTO
/// </summary>
public record class OriginCartDto
{
    /// <summary>
    /// 小车ID
    /// </summary>
    public long? CartId { get; init; }

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTimeOffset LastUpdatedAt { get; init; }
}

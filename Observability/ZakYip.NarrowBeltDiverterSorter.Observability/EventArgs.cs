using System.ComponentModel;

namespace ZakYip.NarrowBeltDiverterSorter.Observability;

/// <summary>
/// 主线速度状态
/// </summary>
public enum LineSpeedStatus
{
    /// <summary>
    /// 未知
    /// </summary>
    [Description("未知")]
    Unknown = 0,

    /// <summary>
    /// 启动中（尚未进入稳速带）
    /// </summary>
    [Description("启动中")]
    Starting = 1,

    /// <summary>
    /// 已稳速
    /// </summary>
    [Description("已稳速")]
    Stable = 2,

    /// <summary>
    /// 速度波动（超出稳速死区）
    /// </summary>
    [Description("速度波动")]
    Unstable = 3
}

/// <summary>
/// 设备状态
/// </summary>
public enum DeviceStatus
{
    /// <summary>
    /// 空闲
    /// </summary>
    [Description("空闲")]
    Idle = 0,

    /// <summary>
    /// 启动中
    /// </summary>
    [Description("启动中")]
    Starting = 1,

    /// <summary>
    /// 运行中
    /// </summary>
    [Description("运行中")]
    Running = 2,

    /// <summary>
    /// 已停止
    /// </summary>
    [Description("已停止")]
    Stopped = 3,

    /// <summary>
    /// 故障
    /// </summary>
    [Description("故障")]
    Faulted = 4,

    /// <summary>
    /// 急停
    /// </summary>
    [Description("急停")]
    EmergencyStopped = 5
}

/// <summary>
/// 主线状态变更事件参数
/// </summary>
public record struct MainLineStateChangedEventArgs
{
    /// <summary>
    /// 主线是否运行
    /// </summary>
    public required bool IsRunning { get; init; }

    /// <summary>
    /// 当前速度 (mm/s)
    /// </summary>
    public required double CurrentSpeed { get; init; }

    /// <summary>
    /// 目标速度 (mm/s)
    /// </summary>
    public required double TargetSpeed { get; init; }

    /// <summary>
    /// 变更时间
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }
}

/// <summary>
/// 小车环构建完成事件参数
/// </summary>
public record struct CartRingBuiltEventArgs
{
    /// <summary>
    /// 小车数量
    /// </summary>
    public required int CartCount { get; init; }

    /// <summary>
    /// 构建时间
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }
}

/// <summary>
/// 主线速度变更事件参数
/// </summary>
public record class LineSpeedChangedEventArgs
{
    /// <summary>
    /// 实际速度 (mm/s)
    /// </summary>
    public required decimal ActualMmps { get; init; }

    /// <summary>
    /// 目标速度 (mm/s)
    /// </summary>
    public required decimal TargetMmps { get; init; }

    /// <summary>
    /// 速度状态
    /// </summary>
    public required LineSpeedStatus Status { get; init; }

    /// <summary>
    /// 发生时间
    /// </summary>
    public required DateTimeOffset OccurredAt { get; init; }
}

/// <summary>
/// 格口下方小车变更事件参数
/// </summary>
public record class CartAtChuteChangedEventArgs
{
    /// <summary>
    /// 格口ID
    /// </summary>
    public required long ChuteId { get; init; }

    /// <summary>
    /// 小车ID（null表示无小车）
    /// </summary>
    public long? CartId { get; init; }

    /// <summary>
    /// 发生时间
    /// </summary>
    public required DateTimeOffset OccurredAt { get; init; }
}

/// <summary>
/// 原点小车变更事件参数
/// </summary>
public record class OriginCartChangedEventArgs
{
    /// <summary>
    /// 小车ID（null表示无小车）
    /// </summary>
    public long? CartId { get; init; }

    /// <summary>
    /// 发生时间
    /// </summary>
    public required DateTimeOffset OccurredAt { get; init; }
}

/// <summary>
/// 包裹创建事件参数
/// </summary>
public record class ParcelCreatedEventArgs
{
    /// <summary>
    /// 包裹ID
    /// </summary>
    public required long ParcelId { get; init; }

    /// <summary>
    /// 条码
    /// </summary>
    public required string Barcode { get; init; }

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
    /// 创建时间
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }
}

/// <summary>
/// 包裹落格事件参数
/// </summary>
public record class ParcelDivertedEventArgs
{
    /// <summary>
    /// 包裹ID
    /// </summary>
    public required long ParcelId { get; init; }

    /// <summary>
    /// 条码
    /// </summary>
    public required string Barcode { get; init; }

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
    /// 落格时间
    /// </summary>
    public required DateTimeOffset DivertedAt { get; init; }
}

/// <summary>
/// 设备状态变更事件参数
/// </summary>
public record class DeviceStatusChangedEventArgs
{
    /// <summary>
    /// 设备状态
    /// </summary>
    public required DeviceStatus Status { get; init; }

    /// <summary>
    /// 状态消息
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// 发生时间
    /// </summary>
    public required DateTimeOffset OccurredAt { get; init; }
}

/// <summary>
/// 小车位置快照
/// </summary>
public record class CartPositionSnapshot
{
    /// <summary>
    /// 小车ID
    /// </summary>
    public required long CartId { get; init; }

    /// <summary>
    /// 小车索引
    /// </summary>
    public required int CartIndex { get; init; }

    /// <summary>
    /// 线性位置 (mm)
    /// </summary>
    public decimal? LinearPositionMm { get; init; }

    /// <summary>
    /// 当前格口ID（如果小车在格口下方）
    /// </summary>
    public long? CurrentChuteId { get; init; }
}

/// <summary>
/// 小车布局变更事件参数
/// </summary>
public record class CartLayoutChangedEventArgs
{
    /// <summary>
    /// 小车位置列表
    /// </summary>
    public required IReadOnlyList<CartPositionSnapshot> CartPositions { get; init; }

    /// <summary>
    /// 格口到小车的映射
    /// </summary>
    public required IReadOnlyDictionary<long, long?> ChuteToCartMapping { get; init; }

    /// <summary>
    /// 发生时间
    /// </summary>
    public required DateTimeOffset OccurredAt { get; init; }
}

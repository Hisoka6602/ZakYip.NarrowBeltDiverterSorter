using System.ComponentModel.DataAnnotations;

namespace ZakYip.NarrowBeltDiverterSorter.Host.DTOs.Requests;

/// <summary>
/// 更新主线控制选项请求
/// </summary>
public sealed record UpdateMainLineControlOptionsRequest
{
    /// <summary>
    /// 目标速度（毫米/秒）
    /// </summary>
    [Required(ErrorMessage = "目标速度不能为空")]
    [Range(0.1, 10000, ErrorMessage = "目标速度必须在 0.1 到 10000 之间")]
    public required decimal TargetSpeedMmps { get; init; }

    /// <summary>
    /// 控制循环周期（毫秒）
    /// </summary>
    [Required(ErrorMessage = "控制循环周期不能为空")]
    [Range(1, 10000, ErrorMessage = "控制循环周期必须在 1 到 10000 之间")]
    public required int LoopPeriodMs { get; init; }

    /// <summary>
    /// PID 控制器比例系数
    /// </summary>
    [Required(ErrorMessage = "比例系数不能为空")]
    public required decimal ProportionalGain { get; init; }

    /// <summary>
    /// PID 控制器积分系数
    /// </summary>
    [Required(ErrorMessage = "积分系数不能为空")]
    public required decimal IntegralGain { get; init; }

    /// <summary>
    /// PID 控制器微分系数
    /// </summary>
    [Required(ErrorMessage = "微分系数不能为空")]
    public required decimal DerivativeGain { get; init; }

    /// <summary>
    /// 稳定判据死区（毫米/秒）
    /// </summary>
    [Required(ErrorMessage = "稳定死区不能为空")]
    [Range(0, 1000, ErrorMessage = "稳定死区必须在 0 到 1000 之间")]
    public required decimal StableDeadbandMmps { get; init; }

    /// <summary>
    /// 稳定判据保持时间（秒）
    /// </summary>
    [Required(ErrorMessage = "稳定保持时间不能为空")]
    [Range(0, 3600, ErrorMessage = "稳定保持时间必须在 0 到 3600 之间")]
    public required int StableHoldSeconds { get; init; }

    /// <summary>
    /// 输出限幅最小值（毫米/秒）
    /// </summary>
    [Required(ErrorMessage = "最小输出不能为空")]
    [Range(0, 10000, ErrorMessage = "最小输出必须在 0 到 10000 之间")]
    public required decimal MinOutputMmps { get; init; }

    /// <summary>
    /// 输出限幅最大值（毫米/秒）
    /// </summary>
    [Required(ErrorMessage = "最大输出不能为空")]
    [Range(0, 10000, ErrorMessage = "最大输出必须在 0 到 10000 之间")]
    public required decimal MaxOutputMmps { get; init; }

    /// <summary>
    /// 积分限幅
    /// </summary>
    [Required(ErrorMessage = "积分限幅不能为空")]
    public required decimal IntegralLimit { get; init; }
}

/// <summary>
/// 更新入口布局选项请求
/// </summary>
public sealed record UpdateInfeedLayoutOptionsRequest
{
    /// <summary>
    /// 入口到主线距离（毫米）
    /// </summary>
    [Required(ErrorMessage = "入口到主线距离不能为空")]
    [Range(1, 100000, ErrorMessage = "入口到主线距离必须在 1 到 100000 之间")]
    public required int InfeedToMainLineDistanceMm { get; init; }

    /// <summary>
    /// 时间容差（毫秒）
    /// </summary>
    [Required(ErrorMessage = "时间容差不能为空")]
    [Range(1, 10000, ErrorMessage = "时间容差必须在 1 到 10000 之间")]
    public required int TimeToleranceMs { get; init; }

    /// <summary>
    /// 小车偏移校准（毫米）
    /// </summary>
    [Required(ErrorMessage = "小车偏移校准不能为空")]
    public required int CartOffsetCalibration { get; init; }
}

/// <summary>
/// 更新上游连接选项请求
/// </summary>
public sealed record UpdateUpstreamConnectionOptionsRequest
{
    /// <summary>
    /// 上游服务基础 URL
    /// </summary>
    [Required(ErrorMessage = "BaseUrl 不能为空")]
    [StringLength(500, ErrorMessage = "BaseUrl 长度不能超过 500")]
    public required string BaseUrl { get; init; }

    /// <summary>
    /// 请求超时时间（秒）
    /// </summary>
    [Required(ErrorMessage = "请求超时时间不能为空")]
    [Range(1, 300, ErrorMessage = "请求超时时间必须在 1 到 300 之间")]
    public required int RequestTimeoutSeconds { get; init; }

    /// <summary>
    /// 认证令牌（可选）
    /// </summary>
    [StringLength(1000, ErrorMessage = "AuthToken 长度不能超过 1000")]
    public string? AuthToken { get; init; }
}

/// <summary>
/// 更新仿真配置请求
/// </summary>
public sealed record UpdateSimulationConfigurationRequest
{
    /// <summary>
    /// 包裹之间的时间间隔（毫秒）
    /// </summary>
    [Required(ErrorMessage = "包裹间隔不能为空")]
    [Range(1, 60000, ErrorMessage = "包裹间隔必须在 1 到 60000 之间")]
    public required int TimeBetweenParcelsMs { get; init; }

    /// <summary>
    /// 总包裹数
    /// </summary>
    [Required(ErrorMessage = "总包裹数不能为空")]
    [Range(1, 1000000, ErrorMessage = "总包裹数必须在 1 到 1000000 之间")]
    public required int TotalParcels { get; init; }

    /// <summary>
    /// 最小包裹长度（毫米）
    /// </summary>
    [Required(ErrorMessage = "最小包裹长度不能为空")]
    [Range(1, 10000, ErrorMessage = "最小包裹长度必须在 1 到 10000 之间")]
    public required int MinParcelLengthMm { get; init; }

    /// <summary>
    /// 最大包裹长度（毫米）
    /// </summary>
    [Required(ErrorMessage = "最大包裹长度不能为空")]
    [Range(1, 10000, ErrorMessage = "最大包裹长度必须在 1 到 10000 之间")]
    public required int MaxParcelLengthMm { get; init; }

    /// <summary>
    /// 随机种子（可选）
    /// </summary>
    public int? RandomSeed { get; init; }

    /// <summary>
    /// 包裹生存时间（秒）
    /// </summary>
    [Required(ErrorMessage = "包裹生存时间不能为空")]
    [Range(1, 86400, ErrorMessage = "包裹生存时间必须在 1 到 86400 之间")]
    public required int ParcelTtlSeconds { get; init; }
}

/// <summary>
/// 更新供包容量配置请求
/// </summary>
public sealed record UpdateFeedingCapacityConfigurationRequest
{
    /// <summary>
    /// 主线上允许的最大在途包裹数
    /// </summary>
    [Required(ErrorMessage = "最大在途包裹数不能为空")]
    [Range(1, 1000, ErrorMessage = "最大在途包裹数必须在 1 到 1000 之间")]
    public required int MaxInFlightParcels { get; init; }

    /// <summary>
    /// 允许等待上游决策的最大请求数
    /// </summary>
    [Required(ErrorMessage = "最大上游等待数不能为空")]
    [Range(1, 1000, ErrorMessage = "最大上游等待数必须在 1 到 1000 之间")]
    public required int MaxUpstreamPendingRequests { get; init; }

    /// <summary>
    /// 供包节流模式（None, SlowDown, Pause）
    /// </summary>
    [Required(ErrorMessage = "节流模式不能为空")]
    [RegularExpression("^(None|SlowDown|Pause)$", ErrorMessage = "节流模式必须是 None, SlowDown 或 Pause")]
    public required string ThrottleMode { get; init; }

    /// <summary>
    /// 降速模式下的延长间隔倍数
    /// </summary>
    [Required(ErrorMessage = "降速倍数不能为空")]
    [Range(1.0, 100.0, ErrorMessage = "降速倍数必须在 1.0 到 100.0 之间")]
    public required double SlowDownMultiplier { get; init; }

    /// <summary>
    /// 在途包裹数低于此阈值时恢复正常供包（可选）
    /// </summary>
    [Range(1, 1000, ErrorMessage = "恢复阈值必须在 1 到 1000 之间")]
    public int? RecoveryThreshold { get; init; }
}

/// <summary>
/// 测试包裹请求
/// </summary>
public sealed record TestParcelRequest
{
    /// <summary>
    /// 包裹条码（必填）
    /// </summary>
    [Required(ErrorMessage = "条码不能为空")]
    [StringLength(200, ErrorMessage = "条码长度不能超过 200")]
    public required string Barcode { get; init; }

    /// <summary>
    /// 包裹ID（可选，不填自动生成）
    /// </summary>
    [StringLength(100, ErrorMessage = "包裹ID长度不能超过 100")]
    public string? ParcelId { get; init; }
}

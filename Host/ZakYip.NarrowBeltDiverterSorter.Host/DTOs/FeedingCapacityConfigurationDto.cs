using System.ComponentModel.DataAnnotations;

namespace ZakYip.NarrowBeltDiverterSorter.Host.DTOs;

/// <summary>
/// 供包容量配置 DTO
/// </summary>
public sealed record FeedingCapacityConfigurationDto
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

using System.ComponentModel.DataAnnotations;
using ZakYip.NarrowBeltDiverterSorter.Core.Enums.Contracts;

namespace ZakYip.NarrowBeltDiverterSorter.Host.DTOs.Recording;

/// <summary>
/// 启动录制会话请求
/// </summary>
public class StartRecordingRequest
{
    /// <summary>
    /// 会话名称（必填）
    /// </summary>
    [Required(ErrorMessage = "会话名称不能为空")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "会话名称长度必须在 1 到 200 之间")]
    public required string Name { get; init; }

    /// <summary>
    /// 会话描述（可选）
    /// </summary>
    [StringLength(1000, ErrorMessage = "会话描述长度不能超过 1000")]
    public string? Description { get; init; }
}

/// <summary>
/// 录制会话响应
/// </summary>
public class RecordingSessionResponse
{
    /// <summary>
    /// 会话ID
    /// </summary>
    public required Guid SessionId { get; init; }

    /// <summary>
    /// 会话名称
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// 开始时间
    /// </summary>
    public required DateTimeOffset StartedAt { get; init; }

    /// <summary>
    /// 停止时间（null表示尚未停止）
    /// </summary>
    public DateTimeOffset? StoppedAt { get; init; }

    /// <summary>
    /// 会话描述
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// 是否正常结束
    /// </summary>
    public required bool IsCompleted { get; init; }

    /// <summary>
    /// 事件计数
    /// </summary>
    public int EventCount { get; init; }
}

/// <summary>
/// 回放请求
/// </summary>
public class ReplayRequest
{
    /// <summary>
    /// 回放模式
    /// </summary>
    [Required(ErrorMessage = "回放模式不能为空")]
    public ReplayMode Mode { get; init; } = ReplayMode.OriginalSpeed;

    /// <summary>
    /// 加速倍数（仅在加速模式下有效）
    /// </summary>
    [Required(ErrorMessage = "加速倍数不能为空")]
    [Range(0.1, 100.0, ErrorMessage = "加速倍数必须在 0.1 到 100.0 之间")]
    public double SpeedFactor { get; init; } = 1.0;

    /// <summary>
    /// 固定间隔毫秒（仅在固定间隔模式下有效）
    /// </summary>
    [Required(ErrorMessage = "固定间隔不能为空")]
    [Range(1, 60000, ErrorMessage = "固定间隔必须在 1 到 60000 毫秒之间")]
    public int FixedIntervalMs { get; init; } = 100;
}

/// <summary>
/// 回放响应
/// </summary>
public class ReplayResponse
{
    /// <summary>
    /// 会话ID
    /// </summary>
    public required Guid SessionId { get; init; }

    /// <summary>
    /// 回放状态
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// 消息
    /// </summary>
    public string? Message { get; init; }
}

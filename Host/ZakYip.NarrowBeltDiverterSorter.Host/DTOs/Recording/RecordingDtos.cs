namespace ZakYip.NarrowBeltDiverterSorter.Host.DTOs.Recording;

/// <summary>
/// 启动录制会话请求
/// </summary>
public class StartRecordingRequest
{
    /// <summary>
    /// 会话名称（必填）
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// 会话描述（可选）
    /// </summary>
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
/// 回放模式
/// </summary>
public enum ReplayMode
{
    /// <summary>
    /// 原速回放
    /// </summary>
    OriginalSpeed,

    /// <summary>
    /// 加速回放
    /// </summary>
    Accelerated,

    /// <summary>
    /// 固定间隔回放
    /// </summary>
    FixedInterval
}

/// <summary>
/// 回放请求
/// </summary>
public class ReplayRequest
{
    /// <summary>
    /// 回放模式
    /// </summary>
    public ReplayMode Mode { get; init; } = ReplayMode.OriginalSpeed;

    /// <summary>
    /// 加速倍数（仅在加速模式下有效）
    /// </summary>
    public double SpeedFactor { get; init; } = 1.0;

    /// <summary>
    /// 固定间隔毫秒（仅在固定间隔模式下有效）
    /// </summary>
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

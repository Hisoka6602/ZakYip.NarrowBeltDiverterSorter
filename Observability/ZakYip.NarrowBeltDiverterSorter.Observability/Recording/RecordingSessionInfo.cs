namespace ZakYip.NarrowBeltDiverterSorter.Observability.Recording;

/// <summary>
/// 录制会话信息
/// </summary>
public record RecordingSessionInfo
{
    /// <summary>
    /// 会话ID
    /// </summary>
    public required Guid SessionId { get; init; }

    /// <summary>
    /// 会话名称（如"2025-11-18_下午班_异常工况"）
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

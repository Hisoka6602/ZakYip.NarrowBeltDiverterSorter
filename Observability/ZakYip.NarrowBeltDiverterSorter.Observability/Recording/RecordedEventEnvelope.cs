namespace ZakYip.NarrowBeltDiverterSorter.Observability.Recording;

/// <summary>
/// 录制事件封装
/// 统一封装各类领域事件，用于录制与回放
/// </summary>
public record struct RecordedEventEnvelope
{
    /// <summary>
    /// 会话ID
    /// </summary>
    public required Guid SessionId { get; init; }

    /// <summary>
    /// 事件发生时间
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// 事件类型（如 "LineSpeedChanged", "ParcelLifecycleChanged", "SafetyStateChanged"）
    /// </summary>
    public required string EventType { get; init; }

    /// <summary>
    /// 事件载荷（序列化为JSON）
    /// </summary>
    public required string PayloadJson { get; init; }

    /// <summary>
    /// 关联ID（可选，用于对接上游调用链）
    /// </summary>
    public string? CorrelationId { get; init; }
}

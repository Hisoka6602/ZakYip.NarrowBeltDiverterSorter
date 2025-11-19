namespace ZakYip.NarrowBeltDiverterSorter.Observability.Events;

/// <summary>
/// 上游请求/响应指标事件参数
/// </summary>
public record class UpstreamMetricsEventArgs
{
    /// <summary>
    /// 总请求次数
    /// </summary>
    public long TotalRequests { get; init; }

    /// <summary>
    /// 成功响应次数
    /// </summary>
    public long SuccessfulResponses { get; init; }

    /// <summary>
    /// 失败响应次数
    /// </summary>
    public long FailedResponses { get; init; }

    /// <summary>
    /// 平均延迟毫秒数
    /// </summary>
    public double AverageLatencyMs { get; init; }

    /// <summary>
    /// 最后一次错误消息
    /// </summary>
    public string? LastError { get; init; }

    /// <summary>
    /// 最后一次错误时间
    /// </summary>
    public DateTimeOffset? LastErrorAt { get; init; }

    /// <summary>
    /// 事件时间戳
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

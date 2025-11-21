using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Upstream;

namespace ZakYip.NarrowBeltDiverterSorter.Observability.LiveView;

/// <summary>
/// 上游规则引擎状态快照
/// </summary>
public record class UpstreamRuleEngineSnapshot
{
    /// <summary>
    /// 当前上游模式（Disabled / Mqtt / Tcp）
    /// </summary>
    public string Mode { get; init; } = "Disabled";

    /// <summary>
    /// 上游连接状态
    /// </summary>
    public UpstreamConnectionStatus Status { get; init; } = UpstreamConnectionStatus.Disabled;

    /// <summary>
    /// 连接地址（如果适用）
    /// </summary>
    public string? ConnectionAddress { get; init; }

    /// <summary>
    /// 最后更新时间（本地时间）
    /// </summary>
    public DateTimeOffset LastUpdatedAt { get; init; } = DateTimeOffset.Now;

    /// <summary>
    /// 总请求次数
    /// </summary>
    public long TotalRequests { get; init; } = 0;

    /// <summary>
    /// 成功响应次数
    /// </summary>
    public long SuccessfulResponses { get; init; } = 0;

    /// <summary>
    /// 失败响应次数
    /// </summary>
    public long FailedResponses { get; init; } = 0;

    /// <summary>
    /// 平均延迟毫秒数（粗略估算）
    /// </summary>
    public double AverageLatencyMs { get; init; } = 0;

    /// <summary>
    /// 最后一次错误消息
    /// </summary>
    public string? LastError { get; init; }

    /// <summary>
    /// 最后一次错误时间
    /// </summary>
    public DateTimeOffset? LastErrorAt { get; init; }
}

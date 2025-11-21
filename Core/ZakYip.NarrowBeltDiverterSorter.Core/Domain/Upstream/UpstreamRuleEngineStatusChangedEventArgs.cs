using ZakYip.NarrowBeltDiverterSorter.Core.Enums.Domain;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.Upstream;

/// <summary>
/// 上游规则引擎状态变更事件参数
/// </summary>
public record class UpstreamRuleEngineStatusChangedEventArgs
{
    /// <summary>
    /// 上游模式（Disabled / Mqtt / Tcp）
    /// </summary>
    public required string Mode { get; init; }

    /// <summary>
    /// 连接状态
    /// </summary>
    public required UpstreamConnectionStatus Status { get; init; }

    /// <summary>
    /// 连接地址（如果适用）
    /// </summary>
    public string? ConnectionAddress { get; init; }

    /// <summary>
    /// 状态变更时间（本地时间）
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.Now;
}

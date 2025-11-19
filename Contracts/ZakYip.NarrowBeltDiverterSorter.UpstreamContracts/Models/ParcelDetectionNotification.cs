namespace ZakYip.NarrowBeltDiverterSorter.UpstreamContracts.Models;

/// <summary>
/// 包裹检测通知
/// 当系统检测到包裹时，发送此通知给上游规则引擎
/// </summary>
public record ParcelDetectionNotification
{
    /// <summary>
    /// 包裹ID（毫秒时间戳）
    /// </summary>
    public required long ParcelId { get; init; }

    /// <summary>
    /// 检测时间
    /// </summary>
    public DateTimeOffset DetectionTime { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// 额外的元数据（可选）
    /// </summary>
    public Dictionary<string, string>? Metadata { get; init; }
}

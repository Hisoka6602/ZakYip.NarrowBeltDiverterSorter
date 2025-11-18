namespace ZakYip.NarrowBeltDiverterSorter.UpstreamContracts.Models;

/// <summary>
/// 包裹创建消息
/// 用于通知规则引擎新包裹已创建（与 RuleEngine 文档对齐）
/// </summary>
public record class ParcelCreatedMessage
{
    /// <summary>
    /// 包裹ID（毫秒时间戳）
    /// </summary>
    public required long ParcelId { get; init; }

    /// <summary>
    /// 小车编号
    /// </summary>
    public required int CartNumber { get; init; }

    /// <summary>
    /// 条码（可选）
    /// </summary>
    public string? Barcode { get; init; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedTime { get; init; } = DateTimeOffset.UtcNow;
}

namespace ZakYip.NarrowBeltDiverterSorter.UpstreamContracts.Models;

/// <summary>
/// 包裹创建消息
/// 用于通知规则引擎新包裹已创建
/// </summary>
/// <remarks>
/// 此消息不引用任何 NarrowBelt 内部类型，只使用基础类型，
/// 保证可以在 RuleEngine Core 那边重用/共享
/// </remarks>
public record class ParcelCreatedMessage
{
    /// <summary>
    /// 包裹ID（毫秒时间戳）
    /// </summary>
    public required long ParcelId { get; init; }

    /// <summary>
    /// 购物车编号（可选）
    /// </summary>
    public int? CartNumber { get; init; }

    /// <summary>
    /// 条码（可选）
    /// </summary>
    public string? Barcode { get; init; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedTime { get; init; } = DateTimeOffset.Now;
}

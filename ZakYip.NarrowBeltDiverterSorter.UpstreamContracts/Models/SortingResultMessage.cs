namespace ZakYip.NarrowBeltDiverterSorter.UpstreamContracts.Models;

/// <summary>
/// 分拣结果消息
/// 用于向规则引擎报告包裹分拣结果
/// </summary>
/// <remarks>
/// 此消息不引用任何 NarrowBelt 内部类型，只使用基础类型，
/// 保证可以在 RuleEngine Core 那边重用/共享
/// 字段类型避免使用 double，使用 decimal 以保证精度
/// </remarks>
public record class SortingResultMessage
{
    /// <summary>
    /// 包裹ID（毫秒时间戳）
    /// </summary>
    public required long ParcelId { get; init; }

    /// <summary>
    /// 格口编号
    /// </summary>
    public required int ChuteNumber { get; init; }

    /// <summary>
    /// 购物车编号（可选）
    /// </summary>
    public int? CartNumber { get; init; }

    /// <summary>
    /// 购物车计数（可选）
    /// </summary>
    public int? CartCount { get; init; }

    /// <summary>
    /// 分拣是否成功
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// 处理时间（毫秒）
    /// </summary>
    public decimal? ProcessingTimeMs { get; init; }

    /// <summary>
    /// 失败原因（如果失败）
    /// </summary>
    public string? FailureReason { get; init; }

    /// <summary>
    /// 结果时间
    /// </summary>
    public DateTimeOffset ResultTime { get; init; } = DateTimeOffset.UtcNow;
}

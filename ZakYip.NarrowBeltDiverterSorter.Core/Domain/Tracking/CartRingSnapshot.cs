namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.Tracking;

/// <summary>
/// 完整小车环的快照
/// </summary>
public record class CartRingSnapshot
{
    /// <summary>
    /// 环长度（小车总数量）
    /// </summary>
    public required RingLength RingLength { get; init; }

    /// <summary>
    /// 0号小车ID
    /// </summary>
    public required CartId ZeroCartId { get; init; }

    /// <summary>
    /// 0号小车索引
    /// </summary>
    public required CartIndex ZeroIndex { get; init; }

    /// <summary>
    /// 小车ID列表
    /// </summary>
    public required IReadOnlyList<CartId> CartIds { get; init; }

    /// <summary>
    /// 构建时间
    /// </summary>
    public required DateTimeOffset BuiltAt { get; init; }
}

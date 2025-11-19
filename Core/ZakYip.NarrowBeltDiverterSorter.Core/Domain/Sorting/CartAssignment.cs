namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.Sorting;

/// <summary>
/// 小车分配记录
/// 记录包裹落到哪个小车上，以及目标格口
/// </summary>
public record class CartAssignment
{
    /// <summary>
    /// 包裹ID
    /// </summary>
    public required ParcelId ParcelId { get; init; }

    /// <summary>
    /// 小车ID
    /// </summary>
    public required CartId CartId { get; init; }

    /// <summary>
    /// 目标格口ID
    /// </summary>
    public required ChuteId ChuteId { get; init; }

    /// <summary>
    /// 分配时间
    /// </summary>
    public required DateTimeOffset AssignedAt { get; init; }
}

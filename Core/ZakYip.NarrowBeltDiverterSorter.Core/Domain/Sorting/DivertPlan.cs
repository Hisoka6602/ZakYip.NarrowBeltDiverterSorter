namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.Sorting;

/// <summary>
/// 分流计划
/// 描述小车到达某格口的时间窗口，用于判断何时触发分流动作
/// </summary>
public record class DivertPlan
{
    /// <summary>
    /// 小车ID
    /// </summary>
    public required CartId CartId { get; init; }

    /// <summary>
    /// 格口ID
    /// </summary>
    public required ChuteId ChuteId { get; init; }

    /// <summary>
    /// 时间窗口开始时间
    /// </summary>
    public required DateTimeOffset WindowStart { get; init; }

    /// <summary>
    /// 时间窗口结束时间
    /// </summary>
    public required DateTimeOffset WindowEnd { get; init; }

    /// <summary>
    /// 包裹ID（可选，某些计划可能还没有绑定包裹）
    /// </summary>
    public ParcelId? ParcelId { get; init; }
}

namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.Sorting;

/// <summary>
/// 吐件规划器接口
/// 负责计算小车到达格口的时间窗口，生成分流计划（DivertPlan）
/// </summary>
public interface IEjectPlanner
{
    /// <summary>
    /// 为给定的小车和格口计算到达时间窗口
    /// </summary>
    /// <param name="cartId">小车ID</param>
    /// <param name="chuteId">格口ID</param>
    /// <param name="now">当前时间</param>
    /// <returns>分流计划，如果无法计算则返回null</returns>
    DivertPlan? CalculateDivertPlan(CartId cartId, ChuteId chuteId, DateTimeOffset now);
}

namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.Sorting;

/// <summary>
/// 分拣规划器接口
/// 负责根据当前小车状态、包裹状态、格口配置规划吐件动作
/// </summary>
public interface ISortingPlanner
{
    /// <summary>
    /// 规划吐件计划
    /// </summary>
    /// <param name="now">当前时间</param>
    /// <param name="horizon">规划时间窗口（未来多长时间内的计划）</param>
    /// <returns>吐件计划列表</returns>
    IReadOnlyList<EjectPlan> PlanEjects(DateTimeOffset now, TimeSpan horizon);
}

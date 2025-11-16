namespace ZakYip.NarrowBeltDiverterSorter.Host;

/// <summary>
/// 分拣执行器配置选项
/// </summary>
public class SortingExecutionOptions
{
    /// <summary>
    /// 执行周期（多久执行一次规划）
    /// </summary>
    public TimeSpan ExecutionPeriod { get; set; } = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// 规划时间窗口（规划未来多长时间内的吐件）
    /// </summary>
    public TimeSpan PlanningHorizon { get; set; } = TimeSpan.FromSeconds(5);
}

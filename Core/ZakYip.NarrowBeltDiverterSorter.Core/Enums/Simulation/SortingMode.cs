namespace ZakYip.NarrowBeltDiverterSorter.Core.Enums.Simulation;

/// <summary>
/// 分拣模式
/// </summary>
public enum SortingMode
{
    /// <summary>
    /// 正式分拣模式：通过上游 RuleEngine 分配格口
    /// </summary>
    Normal,

    /// <summary>
    /// 指定落格模式：始终路由到固定格口
    /// </summary>
    FixedChute,

    /// <summary>
    /// 循环格口模式：按格口列表循环分配
    /// </summary>
    RoundRobin
}

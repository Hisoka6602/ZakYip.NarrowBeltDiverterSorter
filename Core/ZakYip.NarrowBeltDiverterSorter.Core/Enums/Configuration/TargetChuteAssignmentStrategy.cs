namespace ZakYip.NarrowBeltDiverterSorter.Core.Enums.Configuration;

/// <summary>
/// 目标格口分配策略。
/// </summary>
public enum TargetChuteAssignmentStrategy
{
    /// <summary>
    /// 随机分配。
    /// </summary>
    Random,

    /// <summary>
    /// 轮询分配。
    /// </summary>
    RoundRobin,

    /// <summary>
    /// 按概率分布分配（预留，待后续实现）。
    /// </summary>
    Weighted
}

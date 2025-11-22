using System.ComponentModel;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Enums.Configuration;

/// <summary>
/// 目标格口分配策略。
/// </summary>
public enum TargetChuteAssignmentStrategy
{
    /// <summary>
    /// 随机分配。
    /// </summary>
    [Description("随机分配")]
    Random,

    /// <summary>
    /// 轮询分配。
    /// </summary>
    [Description("轮询分配")]
    RoundRobin,

    /// <summary>
    /// 按概率分布分配（预留，待后续实现）。
    /// </summary>
    [Description("按概率分配")]
    Weighted
}

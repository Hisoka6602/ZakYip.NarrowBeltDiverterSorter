namespace ZakYip.NarrowBeltDiverterSorter.Core.Configuration;

/// <summary>
/// 目标格口分配策略配置。
/// </summary>
public sealed record TargetChuteAssignmentProfile
{
    /// <summary>
    /// 分配策略类型。
    /// </summary>
    public TargetChuteAssignmentStrategy Strategy { get; init; } = TargetChuteAssignmentStrategy.Random;

    /// <summary>
    /// 随机种子（用于随机策略），若为 null 则使用随机种子。
    /// </summary>
    public int? RandomSeed { get; init; }

    /// <summary>
    /// 创建默认配置（随机策略）。
    /// </summary>
    public static TargetChuteAssignmentProfile CreateDefault()
    {
        return new TargetChuteAssignmentProfile();
    }
}

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

using System.ComponentModel;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Enums.Domain;

/// <summary>
/// 供包决策结果
/// </summary>
public enum FeedingDecision
{
    /// <summary>
    /// 允许创建包裹
    /// </summary>
    [Description("允许")]
    Allow,

    /// <summary>
    /// 建议延迟创建包裹（降速模式）
    /// </summary>
    [Description("延迟")]
    Delay,

    /// <summary>
    /// 拒绝创建包裹（暂停模式）
    /// </summary>
    [Description("拒绝")]
    Reject
}

using System.ComponentModel;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Enums.Contracts;

/// <summary>
/// 回放模式
/// </summary>
public enum ReplayMode
{
    /// <summary>
    /// 原速回放 - 按照录制时的时间间隔回放
    /// </summary>
    [Description("原速回放")]
    OriginalSpeed,

    /// <summary>
    /// 加速回放 - 按指定倍数加速回放
    /// </summary>
    [Description("加速回放")]
    Accelerated,

    /// <summary>
    /// 固定间隔回放 - 以固定时间间隔回放所有事件
    /// </summary>
    [Description("固定间隔回放")]
    FixedInterval
}

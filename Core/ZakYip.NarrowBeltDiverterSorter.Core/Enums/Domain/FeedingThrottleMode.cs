using System.ComponentModel;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Enums.Domain;

/// <summary>
/// 供包节流模式
/// 定义当达到容量限制时的背压策略
/// </summary>
public enum FeedingThrottleMode
{
    /// <summary>
    /// 无节流（仅记录告警）
    /// </summary>
    [Description("无节流")]
    None = 0,

    /// <summary>
    /// 降速（延长供包间隔）
    /// </summary>
    [Description("降速")]
    SlowDown = 1,

    /// <summary>
    /// 暂停（完全停止创建新包裹）
    /// </summary>
    [Description("暂停")]
    Pause = 2
}

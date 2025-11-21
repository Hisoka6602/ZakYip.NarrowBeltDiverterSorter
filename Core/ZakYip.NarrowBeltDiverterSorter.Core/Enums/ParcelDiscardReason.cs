using System.ComponentModel;
using ZakYip.NarrowBeltDiverterSorter.Core.Enums;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Enums;

/// <summary>
/// 包裹丢弃原因
/// </summary>
public enum ParcelDiscardReason
{
    /// <summary>
    /// 无（正常分拣）
    /// </summary>
    [Description("无")]
    None = 0,

    /// <summary>
    /// 主线速度不稳定
    /// </summary>
    [Description("主线速度不稳定")]
    UnstableMainLineSpeed = 1,

    /// <summary>
    /// 超时
    /// </summary>
    [Description("超时")]
    Timeout = 2,

    /// <summary>
    /// 其他原因
    /// </summary>
    [Description("其他")]
    Other = 99
}

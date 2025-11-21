using System.ComponentModel;
using ZakYip.NarrowBeltDiverterSorter.Core.Enums;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Enums;

/// <summary>
/// 小车装载状态
/// </summary>
public enum CartLoadState
{
    /// <summary>
    /// 空载
    /// </summary>
    [Description("空载")]
    Empty = 0,

    /// <summary>
    /// 已装载
    /// </summary>
    [Description("已装载")]
    Loaded = 1,

    /// <summary>
    /// 未知
    /// </summary>
    [Description("未知")]
    Unknown = 2
}

using System.ComponentModel;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Enums;

/// <summary>
/// 包裹路由状态（语义对齐WheelDiverterSorter的Parcel状态）
/// </summary>
public enum ParcelRouteState
{
    /// <summary>
    /// 等待路由分配
    /// </summary>
    [Description("等待路由分配")]
    WaitingForRouting = 0,

    /// <summary>
    /// 已路由（已分配格口）
    /// </summary>
    [Description("已路由")]
    Routed = 1,

    /// <summary>
    /// 分拣中
    /// </summary>
    [Description("分拣中")]
    Sorting = 2,

    /// <summary>
    /// 已分拣
    /// </summary>
    [Description("已分拣")]
    Sorted = 3,

    /// <summary>
    /// 强制弹出
    /// </summary>
    [Description("强制弹出")]
    ForceEjected = 4,

    /// <summary>
    /// 失败
    /// </summary>
    [Description("失败")]
    Failed = 5
}

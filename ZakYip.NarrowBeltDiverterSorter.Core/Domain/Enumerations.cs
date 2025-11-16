using System.ComponentModel;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain;

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

/// <summary>
/// 主线状态
/// </summary>
public enum MainLineStatus
{
    /// <summary>
    /// 已停止
    /// </summary>
    [Description("已停止")]
    Stopped = 0,

    /// <summary>
    /// 启动中
    /// </summary>
    [Description("启动中")]
    Starting = 1,

    /// <summary>
    /// 运行中
    /// </summary>
    [Description("运行中")]
    Running = 2,

    /// <summary>
    /// 停止中
    /// </summary>
    [Description("停止中")]
    Stopping = 3,

    /// <summary>
    /// 故障
    /// </summary>
    [Description("故障")]
    Fault = 4
}

/// <summary>
/// 包裹分拣结果
/// </summary>
public enum ParcelSortingOutcome
{
    /// <summary>
    /// 正常落格
    /// </summary>
    [Description("正常落格")]
    NormalSort = 0,

    /// <summary>
    /// 强排
    /// </summary>
    [Description("强排")]
    ForceEject = 1,

    /// <summary>
    /// 误分
    /// </summary>
    [Description("误分")]
    Missort = 2,

    /// <summary>
    /// 未处理
    /// </summary>
    [Description("未处理")]
    Unprocessed = 3
}

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

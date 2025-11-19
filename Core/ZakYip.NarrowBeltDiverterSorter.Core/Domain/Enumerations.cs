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

/// <summary>
/// 包裹生命周期状态（统一的包裹状态模型，用于可观测性和报告）
/// </summary>
public enum ParcelStatus
{
    /// <summary>
    /// 已创建，尚未上主线
    /// </summary>
    [Description("已创建")]
    Created = 0,

    /// <summary>
    /// 已上主线，在途中
    /// </summary>
    [Description("在途")]
    OnMainline = 1,

    /// <summary>
    /// 已生成分拣计划，等待进入窗口
    /// </summary>
    [Description("计划中")]
    DivertPlanning = 2,

    /// <summary>
    /// 已成功落入目标格口
    /// </summary>
    [Description("已落目标格口")]
    DivertedToTarget = 3,

    /// <summary>
    /// 已落入异常格口（强排口）
    /// </summary>
    [Description("已落异常格口")]
    DivertedToException = 4,

    /// <summary>
    /// 分拣失败（具体原因见 FailureReason）
    /// </summary>
    [Description("失败")]
    Failed = 5,

    /// <summary>
    /// 被业务取消（例如上游指令撤销）
    /// </summary>
    [Description("已取消")]
    Canceled = 6,

    /// <summary>
    /// 超时过期（在途过久或计划过期）
    /// </summary>
    [Description("已过期")]
    Expired = 7
}

/// <summary>
/// 包裹失败原因（当 ParcelStatus 为 Failed、DivertedToException 或 Expired 时的详细原因）
/// </summary>
public enum ParcelFailureReason
{
    /// <summary>
    /// 无失败（成功状态使用）
    /// </summary>
    [Description("无失败")]
    None = 0,

    /// <summary>
    /// 上游指令超时未返回
    /// </summary>
    [Description("上游指令超时")]
    UpstreamTimeout = 1,

    /// <summary>
    /// 等待上游分配结果超时
    /// </summary>
    [Description("等待上游分配结果超时")]
    WaitingUpstreamResultTimeout = 10,

    /// <summary>
    /// 未获得任何有效分拣计划
    /// </summary>
    [Description("无分拣计划")]
    NoPlan = 2,

    /// <summary>
    /// 计划在使用前已经超过有效期 TTL
    /// </summary>
    [Description("计划已过期")]
    PlanExpired = 3,

    /// <summary>
    /// 未在分拣窗口内执行动作
    /// </summary>
    [Description("错过分拣窗口")]
    MissedWindow = 4,

    /// <summary>
    /// 窗口内存在冲突（例如其他包裹或锁格）
    /// </summary>
    [Description("窗口冲突")]
    WindowConflict = 5,

    /// <summary>
    /// PredictedCartId 与实际小车号不在允许容差范围内
    /// </summary>
    [Description("小车不匹配")]
    CartMismatch = 6,

    /// <summary>
    /// 设备故障导致无法执行分拣动作
    /// </summary>
    [Description("设备故障")]
    DeviceFault = 7,

    /// <summary>
    /// 安全停机导致计划中断（由 LineRunState → SafetyStopped 时触发）
    /// </summary>
    [Description("安全停机")]
    SafetyStop = 8,

    /// <summary>
    /// 人工干预导致流程中断
    /// </summary>
    [Description("人工干预")]
    ManualIntervention = 9,

    /// <summary>
    /// 未分类或未来兼容保底原因
    /// </summary>
    [Description("未知原因")]
    Unknown = 99
}

/// <summary>
/// 线体运行状态（统一的线体运行状态机）
/// </summary>
public enum LineRunState
{
    /// <summary>
    /// 未知状态（初始化前或状态不明）
    /// </summary>
    [Description("未知")]
    Unknown = 0,

    /// <summary>
    /// 空闲，主线未运行，允许启动
    /// </summary>
    [Description("空闲")]
    Idle = 1,

    /// <summary>
    /// 启动过程（驱动上电、自检、加速中）
    /// </summary>
    [Description("启动中")]
    Starting = 2,

    /// <summary>
    /// 正常运行
    /// </summary>
    [Description("运行中")]
    Running = 3,

    /// <summary>
    /// 业务层暂时暂停（非安全停机）
    /// </summary>
    [Description("已暂停")]
    Paused = 4,

    /// <summary>
    /// 受控停车（正常停机流程）
    /// </summary>
    [Description("停止中")]
    Stopping = 5,

    /// <summary>
    /// 因安全原因停机（急停、安全门等）
    /// </summary>
    [Description("安全停机")]
    SafetyStopped = 6,

    /// <summary>
    /// 故障状态，需要人工检查和确认
    /// </summary>
    [Description("故障")]
    Faulted = 7,

    /// <summary>
    /// 从故障/安全停机恢复过程中
    /// </summary>
    [Description("恢复中")]
    Recovering = 8
}

/// <summary>
/// 安全子系统状态
/// </summary>
public enum SafetyState
{
    /// <summary>
    /// 安全条件满足，可以正常运行
    /// </summary>
    [Description("安全")]
    Safe = 0,

    /// <summary>
    /// 安全输入不满足（安全门打开、急停按下等）
    /// </summary>
    [Description("安全输入异常")]
    UnsafeInput = 1,

    /// <summary>
    /// 急停触发
    /// </summary>
    [Description("急停触发")]
    EmergencyStop = 2,

    /// <summary>
    /// 驱动故障（如 VFD 报警）
    /// </summary>
    [Description("驱动故障")]
    DriveFault = 3,

    /// <summary>
    /// 外部联锁断开
    /// </summary>
    [Description("联锁断开")]
    InterlockOpen = 4,

    /// <summary>
    /// 未知安全状态
    /// </summary>
    [Description("未知")]
    Unknown = 99
}

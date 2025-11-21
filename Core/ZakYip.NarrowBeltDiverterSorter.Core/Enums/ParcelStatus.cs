using System.ComponentModel;
using ZakYip.NarrowBeltDiverterSorter.Core.Enums;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Enums;

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

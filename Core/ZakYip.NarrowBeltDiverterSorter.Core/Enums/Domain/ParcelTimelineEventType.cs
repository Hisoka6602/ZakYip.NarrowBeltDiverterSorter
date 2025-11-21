using System.ComponentModel;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Enums.Domain;

/// <summary>
/// 包裹生命周期时间线事件类型
/// </summary>
public enum ParcelTimelineEventType
{
    /// <summary>
    /// 包裹创建
    /// </summary>
    [Description("包裹创建")]
    Created = 1,

    /// <summary>
    /// DWS 数据附加
    /// </summary>
    [Description("DWS数据附加")]
    DwsAttached = 2,

    /// <summary>
    /// 上游请求已发送
    /// </summary>
    [Description("上游请求已发送")]
    UpstreamRequestSent = 3,

    /// <summary>
    /// 上游结果已接收
    /// </summary>
    [Description("上游结果已接收")]
    UpstreamResultReceived = 4,

    /// <summary>
    /// 分拣计划已创建
    /// </summary>
    [Description("分拣计划已创建")]
    PlanCreated = 5,

    /// <summary>
    /// 已装载到小车
    /// </summary>
    [Description("已装载到小车")]
    LoadedToCart = 6,

    /// <summary>
    /// 接近格口
    /// </summary>
    [Description("接近格口")]
    ApproachingChute = 7,

    /// <summary>
    /// 已落格
    /// </summary>
    [Description("已落格")]
    DivertedToChute = 8,

    /// <summary>
    /// 落格失败
    /// </summary>
    [Description("落格失败")]
    DivertFailed = 9,

    /// <summary>
    /// 已完成
    /// </summary>
    [Description("已完成")]
    Completed = 10,

    /// <summary>
    /// 已中断
    /// </summary>
    [Description("已中断")]
    Aborted = 11
}

using System.ComponentModel;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Enums.Domain;

/// <summary>
/// 上游请求状态
/// </summary>
public enum UpstreamRequestStatus
{
    /// <summary>
    /// 等待中（已发送请求，尚未收到响应）
    /// </summary>
    [Description("等待中")]
    Pending = 0,

    /// <summary>
    /// 已分配（已收到上游响应并绑定格口）
    /// </summary>
    [Description("已分配")]
    Assigned = 1,

    /// <summary>
    /// 已超时（超过TTL仍未收到响应）
    /// </summary>
    [Description("已超时")]
    TimedOut = 2
}

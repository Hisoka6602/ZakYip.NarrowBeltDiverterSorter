using System.ComponentModel;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Enums.Domain;

/// <summary>
/// 上游规则引擎连接状态
/// </summary>
public enum UpstreamConnectionStatus
{
    /// <summary>
    /// 已禁用（单机仿真模式）
    /// </summary>
    [Description("已禁用")]
    Disabled,

    /// <summary>
    /// 未连接
    /// </summary>
    [Description("未连接")]
    Disconnected,

    /// <summary>
    /// 正在连接
    /// </summary>
    [Description("正在连接")]
    Connecting,

    /// <summary>
    /// 已连接
    /// </summary>
    [Description("已连接")]
    Connected,

    /// <summary>
    /// 错误
    /// </summary>
    [Description("错误")]
    Error
}

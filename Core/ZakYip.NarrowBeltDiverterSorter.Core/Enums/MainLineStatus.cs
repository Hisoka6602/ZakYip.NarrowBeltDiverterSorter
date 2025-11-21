using System.ComponentModel;
using ZakYip.NarrowBeltDiverterSorter.Core.Enums;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Enums;

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

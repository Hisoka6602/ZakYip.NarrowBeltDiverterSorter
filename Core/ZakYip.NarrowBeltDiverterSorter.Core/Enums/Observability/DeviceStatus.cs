using System.ComponentModel;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Enums.Observability;

/// <summary>
/// 设备状态
/// </summary>
public enum DeviceStatus
{
    /// <summary>
    /// 空闲
    /// </summary>
    [Description("空闲")]
    Idle = 0,

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
    /// 已停止
    /// </summary>
    [Description("已停止")]
    Stopped = 3,

    /// <summary>
    /// 故障
    /// </summary>
    [Description("故障")]
    Faulted = 4,

    /// <summary>
    /// 急停
    /// </summary>
    [Description("急停")]
    EmergencyStopped = 5
}

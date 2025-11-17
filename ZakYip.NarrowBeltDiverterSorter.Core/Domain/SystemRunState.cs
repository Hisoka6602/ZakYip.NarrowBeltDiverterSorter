using System.ComponentModel;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain;

/// <summary>
/// 系统运行状态
/// </summary>
public enum SystemRunState
{
    /// <summary>
    /// 就绪（默认状态，允许所有按钮操作）
    /// </summary>
    [Description("就绪")]
    Ready = 0,

    /// <summary>
    /// 运行中
    /// </summary>
    [Description("运行中")]
    Running = 1,

    /// <summary>
    /// 已停止
    /// </summary>
    [Description("已停止")]
    Stopped = 2,

    /// <summary>
    /// 故障（急停未解除）
    /// </summary>
    [Description("故障")]
    Fault = 3
}

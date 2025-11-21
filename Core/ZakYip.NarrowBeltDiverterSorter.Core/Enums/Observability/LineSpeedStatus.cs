using System.ComponentModel;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Enums.Observability;

/// <summary>
/// 主线速度状态
/// </summary>
public enum LineSpeedStatus
{
    /// <summary>
    /// 未知
    /// </summary>
    [Description("未知")]
    Unknown = 0,

    /// <summary>
    /// 启动中（尚未进入稳速带）
    /// </summary>
    [Description("启动中")]
    Starting = 1,

    /// <summary>
    /// 已稳速
    /// </summary>
    [Description("已稳速")]
    Stable = 2,

    /// <summary>
    /// 速度波动（超出稳速死区）
    /// </summary>
    [Description("速度波动")]
    Unstable = 3
}

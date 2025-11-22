using System.ComponentModel;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Enums.Configuration;

/// <summary>
/// 启动模式枚举
/// </summary>
public enum StartupMode
{
    /// <summary>
    /// 正常模式：全部 Worker 启动
    /// </summary>
    [Description("正常模式")]
    Normal,

    /// <summary>
    /// 主线调试模式：只启动主驱控制和原点监控
    /// </summary>
    [Description("主线调试模式")]
    BringupMainline,

    /// <summary>
    /// 入口调试模式：在 mainline 基础上增加入口相关
    /// </summary>
    [Description("入口调试模式")]
    BringupInfeed,

    /// <summary>
    /// 吐件调试模式：在 infeed 基础上增加吐件相关但可关闭上游
    /// </summary>
    [Description("吐件调试模式")]
    BringupChutes,

    /// <summary>
    /// 上游调试模式：只验证上游通讯，不启动真实 IO/驱动
    /// </summary>
    [Description("上游调试模式")]
    BringupUpstream
}

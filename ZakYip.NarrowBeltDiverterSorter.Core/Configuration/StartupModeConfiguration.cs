namespace ZakYip.NarrowBeltDiverterSorter.Core.Configuration;

/// <summary>
/// 启动模式枚举
/// </summary>
public enum StartupMode
{
    /// <summary>
    /// 正常模式：全部 Worker 启动
    /// </summary>
    Normal,

    /// <summary>
    /// 主线调试模式：只启动主驱控制和原点监控
    /// </summary>
    BringupMainline,

    /// <summary>
    /// 入口调试模式：在 mainline 基础上增加入口相关
    /// </summary>
    BringupInfeed,

    /// <summary>
    /// 吐件调试模式：在 infeed 基础上增加吐件相关但可关闭上游
    /// </summary>
    BringupChutes,

    /// <summary>
    /// 上游调试模式：只验证上游通讯，不启动真实 IO/驱动
    /// </summary>
    BringupUpstream
}

/// <summary>
/// 启动模式配置
/// </summary>
public class StartupModeConfiguration
{
    /// <summary>
    /// 当前启动模式
    /// </summary>
    public StartupMode Mode { get; set; } = StartupMode.Normal;

    /// <summary>
    /// 是否启用增强的调试日志
    /// </summary>
    public bool EnableBringupLogging { get; set; } = true;

    /// <summary>
    /// 解析命令行参数
    /// </summary>
    public static StartupModeConfiguration ParseFromArgs(string[] args)
    {
        var config = new StartupModeConfiguration();

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--mode" && i + 1 < args.Length)
            {
                var modeString = args[i + 1].ToLowerInvariant();
                config.Mode = modeString switch
                {
                    "normal" => StartupMode.Normal,
                    "bringup-mainline" => StartupMode.BringupMainline,
                    "bringup-infeed" => StartupMode.BringupInfeed,
                    "bringup-chutes" => StartupMode.BringupChutes,
                    "bringup-upstream" => StartupMode.BringupUpstream,
                    _ => throw new ArgumentException($"未知的启动模式: {args[i + 1]}")
                };
                break;
            }
        }

        return config;
    }

    /// <summary>
    /// 判断是否应该启动主线控制
    /// </summary>
    public bool ShouldStartMainLineControl() => Mode != StartupMode.BringupUpstream; // 上游调试模式不需要

    /// <summary>
    /// 判断是否应该启动原点传感器监控
    /// </summary>
    public bool ShouldStartOriginSensorMonitor() => Mode != StartupMode.BringupUpstream; // 上游调试模式不需要

    /// <summary>
    /// 判断是否应该启动入口传感器监控
    /// </summary>
    public bool ShouldStartInfeedSensorMonitor() => Mode >= StartupMode.BringupInfeed;

    /// <summary>
    /// 判断是否应该启动包裹装载协调器
    /// </summary>
    public bool ShouldStartParcelLoadCoordinator() => Mode >= StartupMode.BringupInfeed;

    /// <summary>
    /// 判断是否应该启动分拣执行工作器
    /// </summary>
    public bool ShouldStartSortingExecutionWorker() => Mode >= StartupMode.BringupChutes;

    /// <summary>
    /// 判断是否应该启动格口IO监视器
    /// </summary>
    public bool ShouldStartChuteIoMonitor() => Mode >= StartupMode.BringupChutes;

    /// <summary>
    /// 判断是否应该启动包裹路由工作器（上游相关）
    /// </summary>
    public bool ShouldStartParcelRoutingWorker() => Mode == StartupMode.Normal;

    /// <summary>
    /// 判断是否应该启动上游调试工作器
    /// </summary>
    public bool ShouldStartUpstreamBringupWorker() => Mode == StartupMode.BringupUpstream;

    /// <summary>
    /// 获取模式描述
    /// </summary>
    public string GetModeDescription() => Mode switch
    {
        StartupMode.Normal => "正常模式（全部 Worker 启动）",
        StartupMode.BringupMainline => "主线调试模式（主驱控制 + 原点监控）",
        StartupMode.BringupInfeed => "入口调试模式（主线 + 原点 + 入口 + 装载协调）",
        StartupMode.BringupChutes => "吐件调试模式（入口基础 + 吐件执行 + 格口IO）",
        StartupMode.BringupUpstream => "上游调试模式（只验证上游通讯，不启动真实 IO/驱动）",
        _ => "未知模式"
    };
}

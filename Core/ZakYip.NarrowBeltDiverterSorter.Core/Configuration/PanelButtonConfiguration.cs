namespace ZakYip.NarrowBeltDiverterSorter.Core.Configuration;

/// <summary>
/// 面板按钮输入地址配置
/// 定义面板按钮在现场总线上的地址映射
/// </summary>
public sealed record PanelButtonConfiguration
{
    /// <summary>
    /// 启动按钮输入地址
    /// </summary>
    public required int StartButtonAddress { get; init; }

    /// <summary>
    /// 停止按钮输入地址
    /// </summary>
    public required int StopButtonAddress { get; init; }

    /// <summary>
    /// 急停按钮输入地址
    /// </summary>
    public required int EmergencyStopButtonAddress { get; init; }

    /// <summary>
    /// 急停复位按钮输入地址
    /// </summary>
    public required int EmergencyResetButtonAddress { get; init; }

    /// <summary>
    /// 按钮监控周期（毫秒），默认 100 毫秒
    /// </summary>
    public int MonitorPeriodMs { get; init; } = 100;

    /// <summary>
    /// 创建默认配置（用于测试）
    /// 注意：StartButtonAddress = 0 表示未配置，系统将无法进入运行状态
    /// </summary>
    public static PanelButtonConfiguration CreateDefault()
    {
        return new PanelButtonConfiguration
        {
            StartButtonAddress = 0,     // 0 表示未配置
            StopButtonAddress = 0,      // 0 表示未配置
            EmergencyStopButtonAddress = 0,  // 0 表示未配置
            EmergencyResetButtonAddress = 0, // 0 表示未配置
            MonitorPeriodMs = 100
        };
    }
}

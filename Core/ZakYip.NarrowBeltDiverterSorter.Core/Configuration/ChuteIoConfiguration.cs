namespace ZakYip.NarrowBeltDiverterSorter.Core.Configuration;

/// <summary>
/// 格口 IO 配置
/// 定义格口硬件的运行模式和基本参数
/// </summary>
public sealed record ChuteIoConfiguration
{
    /// <summary>
    /// 是否启用真实硬件，默认为 false（使用仿真）
    /// </summary>
    public bool IsHardwareEnabled { get; init; } = false;
    
    /// <summary>
    /// 运行模式，默认 "Simulation"
    /// 可选值：Simulation、ZhiQian32Relay 等
    /// </summary>
    public string Mode { get; init; } = "Simulation";
    
    /// <summary>
    /// 格口动作超时时间（毫秒），默认 5000
    /// </summary>
    public int ActionTimeoutMs { get; init; } = 5000;
    
    /// <summary>
    /// 格口复位延迟时间（毫秒），默认 100
    /// </summary>
    public int ResetDelayMs { get; init; } = 100;
    
    /// <summary>
    /// 是否启用格口状态监控，默认为 true
    /// </summary>
    public bool EnableStatusMonitoring { get; init; } = true;
    
    /// <summary>
    /// 状态监控周期（毫秒），默认 500
    /// </summary>
    public int StatusMonitoringPeriodMs { get; init; } = 500;
    
    /// <summary>
    /// 创建默认配置
    /// </summary>
    public static ChuteIoConfiguration CreateDefault()
    {
        return new ChuteIoConfiguration();
    }
}

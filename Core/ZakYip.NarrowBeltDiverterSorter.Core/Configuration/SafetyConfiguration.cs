namespace ZakYip.NarrowBeltDiverterSorter.Core.Configuration;

/// <summary>
/// 安全控制配置
/// 定义系统安全策略和恢复行为
/// </summary>
public sealed record SafetyConfiguration
{
    /// <summary>
    /// 急停超时时间（秒），默认 5 秒
    /// 超过此时间未收到复位信号将触发二次保护
    /// </summary>
    public int EmergencyStopTimeoutSeconds { get; init; } = 5;
    
    /// <summary>
    /// 是否允许自动恢复，默认为 false
    /// 设为 true 时，某些非关键错误可以自动恢复
    /// </summary>
    public bool AllowAutoRecovery { get; init; } = false;
    
    /// <summary>
    /// 自动恢复尝试间隔（秒），默认 10 秒
    /// </summary>
    public int AutoRecoveryIntervalSeconds { get; init; } = 10;
    
    /// <summary>
    /// 最大自动恢复尝试次数，默认 3 次
    /// </summary>
    public int MaxAutoRecoveryAttempts { get; init; } = 3;
    
    /// <summary>
    /// 安全输入检查周期（毫秒），默认 100 毫秒
    /// </summary>
    public int SafetyInputCheckPeriodMs { get; init; } = 100;
    
    /// <summary>
    /// 是否启用格口安全互锁，默认为 true
    /// 防止多个格口同时动作导致冲突
    /// </summary>
    public bool EnableChuteSafetyInterlock { get; init; } = true;
    
    /// <summary>
    /// 格口互锁超时时间（毫秒），默认 5000 毫秒（5秒）
    /// </summary>
    public int ChuteSafetyInterlockTimeoutMs { get; init; } = 5000;
    
    /// <summary>
    /// 创建默认配置
    /// </summary>
    public static SafetyConfiguration CreateDefault()
    {
        return new SafetyConfiguration();
    }
}

namespace ZakYip.NarrowBeltDiverterSorter.Core.Configuration;

/// <summary>
/// 事件录制配置
/// 控制系统事件录制行为的配置选项
/// </summary>
public sealed record RecordingConfiguration
{
    /// <summary>
    /// 是否默认启用录制，默认为 false
    /// </summary>
    public bool EnabledByDefault { get; init; } = false;
    
    /// <summary>
    /// 最大录制会话时长（秒），默认 3600 秒（1小时）
    /// 超过此时长的会话将自动停止
    /// </summary>
    public int MaxSessionDurationSeconds { get; init; } = 3600;
    
    /// <summary>
    /// 单个会话最大事件数量，默认 100000
    /// 超过此数量将停止录制或循环覆盖
    /// </summary>
    public int MaxEventsPerSession { get; init; } = 100000;
    
    /// <summary>
    /// 录制文件保存目录，默认 "Recordings"
    /// </summary>
    public string RecordingsDirectory { get; init; } = "Recordings";
    
    /// <summary>
    /// 是否自动清理旧录制文件，默认为 false
    /// </summary>
    public bool AutoCleanupOldRecordings { get; init; } = false;
    
    /// <summary>
    /// 录制文件保留天数，默认 30 天
    /// 仅当 AutoCleanupOldRecordings 为 true 时生效
    /// </summary>
    public int RecordingRetentionDays { get; init; } = 30;
    
    /// <summary>
    /// 创建默认配置
    /// </summary>
    public static RecordingConfiguration CreateDefault()
    {
        return new RecordingConfiguration();
    }
}

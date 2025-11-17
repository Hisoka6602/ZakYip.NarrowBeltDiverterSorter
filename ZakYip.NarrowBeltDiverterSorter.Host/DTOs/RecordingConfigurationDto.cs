namespace ZakYip.NarrowBeltDiverterSorter.Host.DTOs;

/// <summary>
/// 录制配置DTO
/// </summary>
public sealed record RecordingConfigurationDto
{
    public bool EnabledByDefault { get; set; } = false;
    public int MaxSessionDurationSeconds { get; set; } = 3600;
    public int MaxEventsPerSession { get; set; } = 100000;
    public string RecordingsDirectory { get; set; } = "Recordings";
    public bool AutoCleanupOldRecordings { get; set; } = false;
    public int RecordingRetentionDays { get; set; } = 30;
}

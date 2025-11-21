using System.ComponentModel.DataAnnotations;

namespace ZakYip.NarrowBeltDiverterSorter.Host.DTOs;

/// <summary>
/// 录制配置DTO
/// </summary>
public sealed record RecordingConfigurationDto
{
    [Required(ErrorMessage = "默认启用标志不能为空")]
    public bool EnabledByDefault { get; set; } = false;
    
    [Required(ErrorMessage = "最大会话时长不能为空")]
    [Range(60, 86400, ErrorMessage = "最大会话时长必须在 60 到 86400 秒之间")]
    public int MaxSessionDurationSeconds { get; set; } = 3600;
    
    [Required(ErrorMessage = "每会话最大事件数不能为空")]
    [Range(1000, 10000000, ErrorMessage = "每会话最大事件数必须在 1000 到 10000000 之间")]
    public int MaxEventsPerSession { get; set; } = 100000;
    
    [Required(ErrorMessage = "录制目录不能为空")]
    [StringLength(500, ErrorMessage = "录制目录长度不能超过 500")]
    public string RecordingsDirectory { get; set; } = "Recordings";
    
    [Required(ErrorMessage = "自动清理标志不能为空")]
    public bool AutoCleanupOldRecordings { get; set; } = false;
    
    [Required(ErrorMessage = "录制保留天数不能为空")]
    [Range(1, 365, ErrorMessage = "录制保留天数必须在 1 到 365 之间")]
    public int RecordingRetentionDays { get; set; } = 30;
}

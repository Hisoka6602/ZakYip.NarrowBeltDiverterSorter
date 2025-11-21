using System.ComponentModel.DataAnnotations;

namespace ZakYip.NarrowBeltDiverterSorter.Host.DTOs;

/// <summary>
/// 安全配置DTO
/// </summary>
public sealed record SafetyConfigurationDto
{
    [Required(ErrorMessage = "急停超时时间不能为空")]
    [Range(1, 300, ErrorMessage = "急停超时时间必须在 1 到 300 秒之间")]
    public int EmergencyStopTimeoutSeconds { get; set; } = 5;
    
    [Required(ErrorMessage = "自动恢复标志不能为空")]
    public bool AllowAutoRecovery { get; set; } = false;
    
    [Required(ErrorMessage = "自动恢复间隔不能为空")]
    [Range(1, 3600, ErrorMessage = "自动恢复间隔必须在 1 到 3600 秒之间")]
    public int AutoRecoveryIntervalSeconds { get; set; } = 10;
    
    [Required(ErrorMessage = "最大自动恢复次数不能为空")]
    [Range(0, 100, ErrorMessage = "最大自动恢复次数必须在 0 到 100 之间")]
    public int MaxAutoRecoveryAttempts { get; set; } = 3;
    
    [Required(ErrorMessage = "安全输入检查周期不能为空")]
    [Range(10, 10000, ErrorMessage = "安全输入检查周期必须在 10 到 10000 毫秒之间")]
    public int SafetyInputCheckPeriodMs { get; set; } = 100;
    
    [Required(ErrorMessage = "格口安全联锁标志不能为空")]
    public bool EnableChuteSafetyInterlock { get; set; } = true;
    
    [Required(ErrorMessage = "格口安全联锁超时不能为空")]
    [Range(100, 60000, ErrorMessage = "格口安全联锁超时必须在 100 到 60000 毫秒之间")]
    public int ChuteSafetyInterlockTimeoutMs { get; set; } = 5000;
}

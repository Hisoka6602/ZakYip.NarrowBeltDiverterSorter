namespace ZakYip.NarrowBeltDiverterSorter.Host.DTOs;

/// <summary>
/// 安全配置DTO
/// </summary>
public sealed record SafetyConfigurationDto
{
    public int EmergencyStopTimeoutSeconds { get; set; } = 5;
    public bool AllowAutoRecovery { get; set; } = false;
    public int AutoRecoveryIntervalSeconds { get; set; } = 10;
    public int MaxAutoRecoveryAttempts { get; set; } = 3;
    public int SafetyInputCheckPeriodMs { get; set; } = 100;
    public bool EnableChuteSafetyInterlock { get; set; } = true;
    public int ChuteSafetyInterlockTimeoutMs { get; set; } = 5000;
}

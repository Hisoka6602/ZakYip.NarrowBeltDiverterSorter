namespace ZakYip.NarrowBeltDiverterSorter.Host.DTOs;

/// <summary>
/// SignalR 推送配置DTO
/// </summary>
public sealed record SignalRPushConfigurationDto
{
    public int LineSpeedPushIntervalMs { get; set; } = 200;
    public int ChuteCartPushIntervalMs { get; set; } = 100;
    public int OriginCartPushIntervalMs { get; set; } = 100;
    public int ParcelCreatedPushIntervalMs { get; set; } = 50;
    public int ParcelDivertedPushIntervalMs { get; set; } = 50;
    public int DeviceStatusPushIntervalMs { get; set; } = 500;
    public int CartLayoutPushIntervalMs { get; set; } = 500;
    public int OnlineParcelsPushPeriodMs { get; set; } = 1000;
    public bool EnableOnlineParcelsPush { get; set; } = true;
}

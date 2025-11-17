namespace ZakYip.NarrowBeltDiverterSorter.Core.Configuration;

/// <summary>
/// SignalR 实时推送配置
/// 控制 SignalR Hub 向客户端推送数据的频率和行为
/// </summary>
public sealed record SignalRPushConfiguration
{
    /// <summary>
    /// 主线速度推送最小间隔（毫秒），默认 200ms
    /// </summary>
    public int LineSpeedPushIntervalMs { get; init; } = 200;
    
    /// <summary>
    /// 格口小车推送最小间隔（毫秒），默认 100ms
    /// </summary>
    public int ChuteCartPushIntervalMs { get; init; } = 100;
    
    /// <summary>
    /// 原点小车推送最小间隔（毫秒），默认 100ms
    /// </summary>
    public int OriginCartPushIntervalMs { get; init; } = 100;
    
    /// <summary>
    /// 包裹创建推送最小间隔（毫秒），默认 50ms
    /// </summary>
    public int ParcelCreatedPushIntervalMs { get; init; } = 50;
    
    /// <summary>
    /// 包裹落格推送最小间隔（毫秒），默认 50ms
    /// </summary>
    public int ParcelDivertedPushIntervalMs { get; init; } = 50;
    
    /// <summary>
    /// 设备状态推送最小间隔（毫秒），默认 500ms
    /// </summary>
    public int DeviceStatusPushIntervalMs { get; init; } = 500;
    
    /// <summary>
    /// 小车布局推送最小间隔（毫秒），默认 500ms
    /// </summary>
    public int CartLayoutPushIntervalMs { get; init; } = 500;
    
    /// <summary>
    /// 在线包裹列表推送周期（毫秒），默认 1000ms
    /// </summary>
    public int OnlineParcelsPushPeriodMs { get; init; } = 1000;
    
    /// <summary>
    /// 是否启用在线包裹列表周期推送，默认 true
    /// </summary>
    public bool EnableOnlineParcelsPush { get; init; } = true;
    
    /// <summary>
    /// 创建默认配置
    /// </summary>
    public static SignalRPushConfiguration CreateDefault()
    {
        return new SignalRPushConfiguration();
    }
}

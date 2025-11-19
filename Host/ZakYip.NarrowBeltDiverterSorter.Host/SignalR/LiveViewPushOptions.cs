namespace ZakYip.NarrowBeltDiverterSorter.Host.SignalR;

/// <summary>
/// SignalR 实时推送配置选项
/// </summary>
public class LiveViewPushOptions
{
    /// <summary>
    /// 配置节名称
    /// </summary>
    public const string SectionName = "LiveViewPush";

    /// <summary>
    /// 主线速度推送最小间隔（毫秒），默认 200ms
    /// </summary>
    public int LineSpeedPushIntervalMs { get; set; } = 200;

    /// <summary>
    /// 格口小车推送最小间隔（毫秒），默认 100ms
    /// </summary>
    public int ChuteCartPushIntervalMs { get; set; } = 100;

    /// <summary>
    /// 原点小车推送最小间隔（毫秒），默认 100ms
    /// </summary>
    public int OriginCartPushIntervalMs { get; set; } = 100;

    /// <summary>
    /// 包裹创建推送最小间隔（毫秒），默认 50ms（允许较频繁）
    /// </summary>
    public int ParcelCreatedPushIntervalMs { get; set; } = 50;

    /// <summary>
    /// 包裹落格推送最小间隔（毫秒），默认 50ms（允许较频繁）
    /// </summary>
    public int ParcelDivertedPushIntervalMs { get; set; } = 50;

    /// <summary>
    /// 设备状态推送最小间隔（毫秒），默认 500ms
    /// </summary>
    public int DeviceStatusPushIntervalMs { get; set; } = 500;

    /// <summary>
    /// 小车布局推送最小间隔（毫秒），默认 500ms
    /// </summary>
    public int CartLayoutPushIntervalMs { get; set; } = 500;

    /// <summary>
    /// 在线包裹列表推送周期（毫秒），默认 1000ms（1秒）
    /// 通过定时任务推送，而非事件驱动
    /// </summary>
    public int OnlineParcelsPushPeriodMs { get; set; } = 1000;

    /// <summary>
    /// 是否启用在线包裹列表周期推送，默认 true
    /// </summary>
    public bool EnableOnlineParcelsPush { get; set; } = true;
}

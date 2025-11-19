namespace ZakYip.NarrowBeltDiverterSorter.Observability.LiveView;

/// <summary>
/// 窄带分拣机实时视图接口
/// 提供系统当前状态的只读快照
/// </summary>
public interface INarrowBeltLiveView
{
    /// <summary>
    /// 获取主线速度快照
    /// </summary>
    LineSpeedSnapshot GetLineSpeed();

    /// <summary>
    /// 获取最后创建的包裹
    /// </summary>
    ParcelSummary? GetLastCreatedParcel();

    /// <summary>
    /// 获取最后落格的包裹
    /// </summary>
    ParcelSummary? GetLastDivertedParcel();

    /// <summary>
    /// 获取当前在线的包裹列表
    /// </summary>
    IReadOnlyCollection<ParcelSummary> GetOnlineParcels();

    /// <summary>
    /// 获取设备状态快照
    /// </summary>
    DeviceStatusSnapshot GetDeviceStatus();

    /// <summary>
    /// 获取原点小车快照
    /// </summary>
    OriginCartSnapshot GetOriginCart();

    /// <summary>
    /// 获取格口小车映射快照
    /// </summary>
    ChuteCartSnapshot GetChuteCarts();

    /// <summary>
    /// 获取指定格口的小车ID
    /// </summary>
    long? GetChuteCart(long chuteId);

    /// <summary>
    /// 获取小车布局快照
    /// </summary>
    CartLayoutSnapshot GetCartLayout();

    /// <summary>
    /// 获取线体运行状态快照
    /// </summary>
    LineRunStateSnapshot GetLineRunState();

    /// <summary>
    /// 获取安全状态快照
    /// </summary>
    SafetyStateSnapshot GetSafetyState();

    /// <summary>
    /// 获取上游规则引擎状态快照
    /// </summary>
    UpstreamRuleEngineSnapshot GetUpstreamRuleEngineStatus();

    /// <summary>
    /// 获取最后分拣请求快照
    /// </summary>
    LastSortingRequestSnapshot? GetLastSortingRequest();

    /// <summary>
    /// 获取最后分拣结果快照
    /// </summary>
    LastSortingResultSnapshot? GetLastSortingResult();

    /// <summary>
    /// 获取供包容量快照
    /// </summary>
    FeedingCapacitySnapshot GetFeedingCapacity();
}

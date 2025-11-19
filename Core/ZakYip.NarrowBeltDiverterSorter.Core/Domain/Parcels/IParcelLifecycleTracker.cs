namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.Parcels;

/// <summary>
/// 包裹生命周期追踪器接口
/// 维护包裹的完整生命周期状态，用于可观测性和调试
/// </summary>
public interface IParcelLifecycleTracker
{
    /// <summary>
    /// 包裹生命周期状态变化事件（已废弃，请订阅 IEventBus）
    /// </summary>
    [Obsolete("请使用 IEventBus 订阅 Observability.Events.ParcelLifecycleChangedEventArgs，此事件将在未来版本中移除")]
    event EventHandler<ParcelLifecycleChangedEventArgs>? LifecycleChanged;

    /// <summary>
    /// 更新包裹的生命周期状态
    /// </summary>
    /// <param name="parcelId">包裹ID</param>
    /// <param name="status">新的生命周期状态</param>
    /// <param name="failureReason">失败原因（可选）</param>
    /// <param name="remarks">备注（可选）</param>
    void UpdateStatus(ParcelId parcelId, ParcelStatus status, ParcelFailureReason failureReason = ParcelFailureReason.None, string? remarks = null);

    /// <summary>
    /// 获取包裹的当前生命周期状态
    /// </summary>
    /// <param name="parcelId">包裹ID</param>
    /// <returns>包裹快照，如果不存在返回 null</returns>
    ParcelSnapshot? GetParcelSnapshot(ParcelId parcelId);

    /// <summary>
    /// 获取所有在线包裹（未完成的包裹）
    /// </summary>
    /// <returns>在线包裹快照列表</returns>
    IReadOnlyList<ParcelSnapshot> GetOnlineParcels();

    /// <summary>
    /// 获取最近完成的包裹列表
    /// </summary>
    /// <param name="count">返回的数量</param>
    /// <returns>最近完成的包裹快照列表</returns>
    IReadOnlyList<ParcelSnapshot> GetRecentCompletedParcels(int count = 100);

    /// <summary>
    /// 获取按状态分组的统计信息
    /// </summary>
    /// <returns>状态分布字典</returns>
    IReadOnlyDictionary<ParcelStatus, int> GetStatusDistribution();

    /// <summary>
    /// 获取按失败原因分组的统计信息
    /// </summary>
    /// <returns>失败原因分布字典</returns>
    IReadOnlyDictionary<ParcelFailureReason, int> GetFailureReasonDistribution();

    /// <summary>
    /// 清除历史记录（通常用于长时间运行后的内存管理）
    /// </summary>
    /// <param name="keepRecentCount">保留最近的记录数量</param>
    void ClearHistory(int keepRecentCount = 100);

    /// <summary>
    /// 获取当前在途包裹数量（未完成的包裹）
    /// 用于供包背压控制
    /// </summary>
    /// <returns>在途包裹数量</returns>
    int GetInFlightCount();

    /// <summary>
    /// 获取当前等待上游决策的包裹数量
    /// 用于防止上游规则引擎过载
    /// </summary>
    /// <returns>等待上游决策的包裹数量</returns>
    int GetUpstreamPendingCount();
}

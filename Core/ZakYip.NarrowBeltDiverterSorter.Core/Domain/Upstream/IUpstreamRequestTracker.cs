namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.Upstream;

/// <summary>
/// 上游请求追踪服务接口
/// 管理上游格口分配请求的生命周期
/// </summary>
public interface IUpstreamRequestTracker
{
    /// <summary>
    /// 记录新的上游请求
    /// </summary>
    /// <param name="parcelId">包裹ID</param>
    /// <param name="requestedAt">请求时间</param>
    /// <param name="deadline">截止时间</param>
    void RecordRequest(ParcelId parcelId, DateTimeOffset requestedAt, DateTimeOffset deadline);

    /// <summary>
    /// 标记请求已分配（收到上游响应）
    /// </summary>
    /// <param name="parcelId">包裹ID</param>
    /// <param name="chuteId">分配的格口ID</param>
    /// <param name="respondedAt">响应时间</param>
    /// <returns>如果成功标记返回true，如果请求不存在或已超时返回false</returns>
    bool MarkAssigned(ParcelId parcelId, ChuteId chuteId, DateTimeOffset respondedAt);

    /// <summary>
    /// 标记请求已超时
    /// </summary>
    /// <param name="parcelId">包裹ID</param>
    /// <param name="timedOutAt">超时时间</param>
    void MarkTimedOut(ParcelId parcelId, DateTimeOffset timedOutAt);

    /// <summary>
    /// 获取请求记录
    /// </summary>
    /// <param name="parcelId">包裹ID</param>
    /// <returns>请求记录，如果不存在返回null</returns>
    UpstreamRequestRecord? GetRecord(ParcelId parcelId);

    /// <summary>
    /// 获取所有待处理的请求（状态为Pending）
    /// </summary>
    /// <returns>待处理的请求记录列表</returns>
    IReadOnlyList<UpstreamRequestRecord> GetPendingRequests();

    /// <summary>
    /// 获取所有已超时的请求（Deadline已过但状态仍为Pending）
    /// </summary>
    /// <param name="currentTime">当前时间</param>
    /// <returns>已超时的请求记录列表</returns>
    IReadOnlyList<UpstreamRequestRecord> GetTimedOutRequests(DateTimeOffset currentTime);
}

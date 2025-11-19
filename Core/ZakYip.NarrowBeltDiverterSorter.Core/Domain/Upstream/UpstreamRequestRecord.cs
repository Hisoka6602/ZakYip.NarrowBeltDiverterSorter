namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.Upstream;

/// <summary>
/// 上游请求状态
/// </summary>
public enum UpstreamRequestStatus
{
    /// <summary>
    /// 等待中（已发送请求，尚未收到响应）
    /// </summary>
    Pending = 0,

    /// <summary>
    /// 已分配（已收到上游响应并绑定格口）
    /// </summary>
    Assigned = 1,

    /// <summary>
    /// 已超时（超过TTL仍未收到响应）
    /// </summary>
    TimedOut = 2
}

/// <summary>
/// 上游请求记录
/// 用于追踪包裹的上游格口分配请求状态
/// </summary>
public class UpstreamRequestRecord
{
    /// <summary>
    /// 包裹ID
    /// </summary>
    public required ParcelId ParcelId { get; init; }

    /// <summary>
    /// 请求时间
    /// </summary>
    public required DateTimeOffset RequestedAt { get; init; }

    /// <summary>
    /// 截止时间（RequestedAt + TTL）
    /// </summary>
    public required DateTimeOffset Deadline { get; init; }

    /// <summary>
    /// 请求状态
    /// </summary>
    public UpstreamRequestStatus Status { get; set; } = UpstreamRequestStatus.Pending;

    /// <summary>
    /// 分配的格口ID（仅当Status为Assigned时有效）
    /// </summary>
    public ChuteId? AssignedChuteId { get; set; }

    /// <summary>
    /// 响应时间（收到上游响应或标记超时的时间）
    /// </summary>
    public DateTimeOffset? RespondedAt { get; set; }
}

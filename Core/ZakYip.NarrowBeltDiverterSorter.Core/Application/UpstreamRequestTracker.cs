using System.Collections.Concurrent;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Upstream;
using ZakYip.NarrowBeltDiverterSorter.Core.Enums.Domain;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Application;

/// <summary>
/// 上游请求追踪服务实现
/// 使用内存存储管理上游请求记录
/// </summary>
public class UpstreamRequestTracker : IUpstreamRequestTracker
{
    private readonly ConcurrentDictionary<ParcelId, UpstreamRequestRecord> _records = new();

    /// <inheritdoc/>
    public void RecordRequest(ParcelId parcelId, DateTimeOffset requestedAt, DateTimeOffset deadline)
    {
        var record = new UpstreamRequestRecord
        {
            ParcelId = parcelId,
            RequestedAt = requestedAt,
            Deadline = deadline,
            Status = UpstreamRequestStatus.Pending
        };

        if (!_records.TryAdd(parcelId, record))
        {
            throw new InvalidOperationException($"包裹 {parcelId.Value} 的上游请求记录已存在");
        }
    }

    /// <inheritdoc/>
    public bool MarkAssigned(ParcelId parcelId, ChuteId chuteId, DateTimeOffset respondedAt)
    {
        if (!_records.TryGetValue(parcelId, out var record))
        {
            return false; // 记录不存在
        }

        // 检查是否已超时
        if (respondedAt > record.Deadline)
        {
            return false; // 已超时，不接受迟到的响应
        }

        // 检查状态是否为Pending
        if (record.Status != UpstreamRequestStatus.Pending)
        {
            return false; // 状态不是Pending，不能标记为Assigned
        }

        // 更新状态
        record.Status = UpstreamRequestStatus.Assigned;
        record.AssignedChuteId = chuteId;
        record.RespondedAt = respondedAt;

        return true;
    }

    /// <inheritdoc/>
    public void MarkTimedOut(ParcelId parcelId, DateTimeOffset timedOutAt)
    {
        if (!_records.TryGetValue(parcelId, out var record))
        {
            throw new InvalidOperationException($"包裹 {parcelId.Value} 的上游请求记录不存在");
        }

        record.Status = UpstreamRequestStatus.TimedOut;
        record.RespondedAt = timedOutAt;
    }

    /// <inheritdoc/>
    public UpstreamRequestRecord? GetRecord(ParcelId parcelId)
    {
        return _records.TryGetValue(parcelId, out var record) ? record : null;
    }

    /// <inheritdoc/>
    public IReadOnlyList<UpstreamRequestRecord> GetPendingRequests()
    {
        return _records.Values
            .Where(r => r.Status == UpstreamRequestStatus.Pending)
            .ToList();
    }

    /// <inheritdoc/>
    public IReadOnlyList<UpstreamRequestRecord> GetTimedOutRequests(DateTimeOffset currentTime)
    {
        return _records.Values
            .Where(r => r.Status == UpstreamRequestStatus.Pending && currentTime > r.Deadline)
            .ToList();
    }
}

using System.Collections.Concurrent;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Parcels;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Application;

/// <summary>
/// 包裹生命周期追踪器实现
/// 使用内存存储跟踪包裹生命周期状态
/// </summary>
public class ParcelLifecycleTracker : IParcelLifecycleTracker
{
    private readonly ConcurrentDictionary<ParcelId, ParcelSnapshot> _parcels = new();
    private readonly IParcelLifecycleService _parcelLifecycleService;
    private readonly object _lockObject = new();

    // 用于存储已完成包裹的历史记录（限制大小）
    private readonly Queue<ParcelSnapshot> _completedHistory = new();
    private const int DefaultMaxHistorySize = 1000;
    private readonly int _maxHistorySize;

    /// <inheritdoc/>
    public event EventHandler<ParcelLifecycleChangedEventArgs>? LifecycleChanged;

    public ParcelLifecycleTracker(
        IParcelLifecycleService parcelLifecycleService,
        int maxHistorySize = DefaultMaxHistorySize)
    {
        _parcelLifecycleService = parcelLifecycleService;
        _maxHistorySize = maxHistorySize;
    }

    /// <inheritdoc/>
    public void UpdateStatus(
        ParcelId parcelId,
        ParcelStatus status,
        ParcelFailureReason failureReason = ParcelFailureReason.None,
        string? remarks = null)
    {
        var occurredAt = DateTimeOffset.UtcNow;

        // 从底层服务获取当前包裹快照
        var currentSnapshot = _parcelLifecycleService.Get(parcelId);
        if (currentSnapshot == null)
        {
            // 如果包裹不存在，记录警告但不抛出异常（容错处理）
            return;
        }

        // 创建更新后的快照
        var updatedSnapshot = currentSnapshot with
        {
            Status = status,
            FailureReason = failureReason
        };

        // 根据状态更新时间戳
        updatedSnapshot = status switch
        {
            ParcelStatus.OnMainline when !updatedSnapshot.LoadedAt.HasValue =>
                updatedSnapshot with { LoadedAt = occurredAt },
            ParcelStatus.DivertPlanning when !updatedSnapshot.DivertPlannedAt.HasValue =>
                updatedSnapshot with { DivertPlannedAt = occurredAt },
            ParcelStatus.DivertedToTarget or ParcelStatus.DivertedToException =>
                updatedSnapshot with
                {
                    DivertedAt = updatedSnapshot.DivertedAt ?? occurredAt,
                    CompletedAt = occurredAt
                },
            ParcelStatus.Failed or ParcelStatus.Canceled or ParcelStatus.Expired =>
                updatedSnapshot with { CompletedAt = occurredAt },
            _ => updatedSnapshot
        };

        // 更新内存中的快照
        _parcels.AddOrUpdate(parcelId, updatedSnapshot, (_, _) => updatedSnapshot);

        // 如果包裹已完成，移到历史记录并从在线字典中移除
        if (IsCompletedStatus(status))
        {
            lock (_lockObject)
            {
                _completedHistory.Enqueue(updatedSnapshot);
                // 限制历史记录大小
                while (_completedHistory.Count > _maxHistorySize)
                {
                    _completedHistory.Dequeue();
                }
            }
            
            // 从在线字典中移除已完成的包裹
            _parcels.TryRemove(parcelId, out _);
        }

        // 发布生命周期变化事件
        LifecycleChanged?.Invoke(this, new ParcelLifecycleChangedEventArgs
        {
            ParcelId = parcelId,
            Status = status,
            FailureReason = failureReason,
            OccurredAt = occurredAt,
            Remarks = remarks
        });
    }

    /// <inheritdoc/>
    public ParcelSnapshot? GetParcelSnapshot(ParcelId parcelId)
    {
        return _parcels.TryGetValue(parcelId, out var snapshot) ? snapshot : null;
    }

    /// <inheritdoc/>
    public IReadOnlyList<ParcelSnapshot> GetOnlineParcels()
    {
        return _parcels.Values
            .Where(p => !IsCompletedStatus(p.Status))
            .OrderBy(p => p.CreatedAt)
            .ToList();
    }

    /// <inheritdoc/>
    public IReadOnlyList<ParcelSnapshot> GetRecentCompletedParcels(int count = 100)
    {
        lock (_lockObject)
        {
            return _completedHistory
                .TakeLast(count)
                .Reverse()
                .ToList();
        }
    }

    /// <inheritdoc/>
    public IReadOnlyDictionary<ParcelStatus, int> GetStatusDistribution()
    {
        var distribution = new Dictionary<ParcelStatus, int>();
        
        // 统计在线包裹
        foreach (var parcel in _parcels.Values)
        {
            if (!distribution.ContainsKey(parcel.Status))
            {
                distribution[parcel.Status] = 0;
            }
            distribution[parcel.Status]++;
        }

        // 统计历史包裹
        lock (_lockObject)
        {
            foreach (var parcel in _completedHistory)
            {
                if (!distribution.ContainsKey(parcel.Status))
                {
                    distribution[parcel.Status] = 0;
                }
                distribution[parcel.Status]++;
            }
        }

        return distribution;
    }

    /// <inheritdoc/>
    public IReadOnlyDictionary<ParcelFailureReason, int> GetFailureReasonDistribution()
    {
        var distribution = new Dictionary<ParcelFailureReason, int>();

        // 统计在线包裹
        foreach (var parcel in _parcels.Values.Where(p => p.FailureReason != ParcelFailureReason.None))
        {
            if (!distribution.ContainsKey(parcel.FailureReason))
            {
                distribution[parcel.FailureReason] = 0;
            }
            distribution[parcel.FailureReason]++;
        }

        // 统计历史包裹
        lock (_lockObject)
        {
            foreach (var parcel in _completedHistory.Where(p => p.FailureReason != ParcelFailureReason.None))
            {
                if (!distribution.ContainsKey(parcel.FailureReason))
                {
                    distribution[parcel.FailureReason] = 0;
                }
                distribution[parcel.FailureReason]++;
            }
        }

        return distribution;
    }

    /// <inheritdoc/>
    public void ClearHistory(int keepRecentCount = 100)
    {
        lock (_lockObject)
        {
            while (_completedHistory.Count > keepRecentCount)
            {
                _completedHistory.Dequeue();
            }
        }
        
        // No need to remove from _parcels as completed parcels are already removed during UpdateStatus
    }

    private static bool IsCompletedStatus(ParcelStatus status)
    {
        return status is ParcelStatus.DivertedToTarget
            or ParcelStatus.DivertedToException
            or ParcelStatus.Failed
            or ParcelStatus.Canceled
            or ParcelStatus.Expired;
    }
}

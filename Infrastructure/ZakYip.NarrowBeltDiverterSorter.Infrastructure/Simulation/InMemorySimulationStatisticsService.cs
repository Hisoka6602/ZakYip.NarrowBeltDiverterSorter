using System.Collections.Concurrent;

namespace ZakYip.NarrowBeltDiverterSorter.Infrastructure.Simulation;

/// <summary>
/// 仿真统计服务的内存实现
/// </summary>
/// <remarks>
/// 使用内存字典存储统计数据，适用于单次运行的仿真测试
/// </remarks>
public class InMemorySimulationStatisticsService : ISimulationStatisticsService
{
    private readonly ConcurrentDictionary<string, RunStatistics> _runs = new();
    private string? _activeRunId;

    public void StartRun(string runId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);

        _activeRunId = runId;
        _runs[runId] = new RunStatistics
        {
            RunId = runId,
            StartTime = DateTimeOffset.UtcNow,
            IsCompleted = false
        };
    }

    public void EndRun(string runId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);

        if (_runs.TryGetValue(runId, out var stats))
        {
            _runs[runId] = stats with
            {
                IsCompleted = true,
                EndTime = DateTimeOffset.UtcNow
            };
        }

        if (_activeRunId == runId)
        {
            _activeRunId = null;
        }
    }

    public void RecordParcelCreated(string runId, long parcelId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);

        if (_runs.TryGetValue(runId, out var stats))
        {
            stats.ParcelIds.Add(parcelId);
            _runs[runId] = stats with { TotalParcels = stats.TotalParcels + 1 };
        }
    }

    public void RecordParcelSorted(string runId, long parcelId, int targetChuteId, int actualChuteId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);

        if (_runs.TryGetValue(runId, out var stats))
        {
            if (targetChuteId == actualChuteId)
            {
                _runs[runId] = stats with { SortedToTargetChutes = stats.SortedToTargetChutes + 1 };
            }
            else
            {
                _runs[runId] = stats with { MisSortedCount = stats.MisSortedCount + 1 };
            }
        }
    }

    public void RecordParcelToErrorChute(string runId, long parcelId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);

        if (_runs.TryGetValue(runId, out var stats))
        {
            _runs[runId] = stats with { SortedToErrorChute = stats.SortedToErrorChute + 1 };
        }
    }

    public void RecordParcelTimedOut(string runId, long parcelId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);

        if (_runs.TryGetValue(runId, out var stats))
        {
            stats.TimedOutParcelIds.Add(parcelId);
            _runs[runId] = stats with { TimedOutCount = stats.TimedOutCount + 1 };
        }
    }

    public SimulationStatistics? GetStatistics(string runId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);

        if (_runs.TryGetValue(runId, out var stats))
        {
            return new SimulationStatistics
            {
                RunId = stats.RunId,
                TotalParcels = stats.TotalParcels,
                SortedToTargetChutes = stats.SortedToTargetChutes,
                SortedToErrorChute = stats.SortedToErrorChute,
                TimedOutCount = stats.TimedOutCount,
                MisSortedCount = stats.MisSortedCount,
                IsCompleted = stats.IsCompleted,
                StartTime = stats.StartTime,
                EndTime = stats.EndTime
            };
        }

        return null;
    }

    public string? GetActiveRunId()
    {
        return _activeRunId;
    }

    private record RunStatistics
    {
        public required string RunId { get; init; }
        public int TotalParcels { get; init; }
        public int SortedToTargetChutes { get; init; }
        public int SortedToErrorChute { get; init; }
        public int TimedOutCount { get; init; }
        public int MisSortedCount { get; init; }
        public bool IsCompleted { get; init; }
        public DateTimeOffset? StartTime { get; init; }
        public DateTimeOffset? EndTime { get; init; }

        public ConcurrentBag<long> ParcelIds { get; } = new();
        public ConcurrentBag<long> TimedOutParcelIds { get; } = new();
    }
}

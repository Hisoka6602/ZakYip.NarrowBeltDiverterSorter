using System.Collections.Concurrent;

namespace ZakYip.NarrowBeltDiverterSorter.Simulation;

/// <summary>
/// 内存实现的窄带分拣仿真报告服务。
/// 适用于开发和测试环境。
/// </summary>
public class InMemoryNarrowBeltSimulationReportService : INarrowBeltSimulationReportService
{
    private readonly ConcurrentDictionary<string, SimulationReport> _reports = new();

    /// <inheritdoc/>
    public Task SaveReportAsync(string runId, SimulationReport report, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(runId))
        {
            throw new ArgumentException("运行ID不能为空", nameof(runId));
        }

        if (report == null)
        {
            throw new ArgumentNullException(nameof(report));
        }

        _reports[runId] = report;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<SimulationReport?> GetReportAsync(string runId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(runId))
        {
            throw new ArgumentException("运行ID不能为空", nameof(runId));
        }

        _reports.TryGetValue(runId, out var report);
        return Task.FromResult(report);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<string>> GetAllRunIdsAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<string> runIds = _reports.Keys.OrderByDescending(k => k).ToList();
        return Task.FromResult(runIds);
    }

    /// <inheritdoc/>
    public Task DeleteReportAsync(string runId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(runId))
        {
            throw new ArgumentException("运行ID不能为空", nameof(runId));
        }

        _reports.TryRemove(runId, out _);
        return Task.CompletedTask;
    }
}

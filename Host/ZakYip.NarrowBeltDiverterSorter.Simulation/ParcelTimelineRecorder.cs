using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Parcels;
using ZakYip.NarrowBeltDiverterSorter.Core.Enums;

namespace ZakYip.NarrowBeltDiverterSorter.Simulation;

/// <summary>
/// 包裹生命周期记录器，用于记录包裹在仿真中的关键事件。
/// </summary>
public class ParcelTimelineRecorder
{
    private readonly ILogger<ParcelTimelineRecorder> _logger;
    private readonly ConcurrentDictionary<long, List<TimelineEvent>> _timelines = new();
    private readonly DateTimeOffset _startTime;

    public ParcelTimelineRecorder(ILogger<ParcelTimelineRecorder> logger)
    {
        _logger = logger;
        _startTime = DateTimeOffset.Now;
    }

    /// <summary>
    /// 记录事件。
    /// </summary>
    public void RecordEvent(ParcelId parcelId, string eventLabel, string? details = null)
    {
        var evt = new TimelineEvent
        {
            Timestamp = DateTimeOffset.Now,
            ElapsedMs = (DateTimeOffset.Now - _startTime).TotalMilliseconds,
            Label = eventLabel,
            Details = details
        };

        _timelines.AddOrUpdate(
            parcelId.Value,
            _ => new List<TimelineEvent> { evt },
            (_, list) =>
            {
                list.Add(evt);
                return list;
            });
    }

    /// <summary>
    /// 获取指定包裹的时间线。
    /// </summary>
    public IReadOnlyList<TimelineEvent>? GetTimeline(ParcelId parcelId)
    {
        return _timelines.TryGetValue(parcelId.Value, out var timeline) ? timeline : null;
    }

    /// <summary>
    /// 获取所有包裹ID。
    /// </summary>
    public IEnumerable<long> GetAllParcelIds()
    {
        return _timelines.Keys;
    }

    /// <summary>
    /// 生成Markdown报告。
    /// </summary>
    public string GenerateMarkdownReport(
        IParcelLifecycleService parcelLifecycleService,
        string configSummary)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("# 长时间高负载分拣稳定性仿真报告");
        sb.AppendLine();
        sb.AppendLine("## 仿真配置");
        sb.AppendLine(configSummary);
        sb.AppendLine();
        
        var allParcels = parcelLifecycleService.GetAll();
        var normalSorts = allParcels.Count(p => p.SortingOutcome == ParcelSortingOutcome.NormalSort);
        var forceEjects = allParcels.Count(p => p.SortingOutcome == ParcelSortingOutcome.ForceEject);
        var missorts = allParcels.Count(p => p.SortingOutcome == ParcelSortingOutcome.Missort);
        var unprocessed = allParcels.Count(p => p.SortingOutcome == ParcelSortingOutcome.Unprocessed || p.SortingOutcome == null);
        
        sb.AppendLine("## 仿真统计");
        sb.AppendLine($"- 总包裹数: {allParcels.Count}");
        sb.AppendLine($"- 正常落格: {normalSorts}");
        sb.AppendLine($"- 异常落格(异常口): {forceEjects}");
        sb.AppendLine($"- 错分: {missorts}");
        sb.AppendLine($"- 未完成: {unprocessed}");
        sb.AppendLine();
        
        sb.AppendLine("## 包裹生命周期");
        sb.AppendLine();
        
        // 只输出前10个和后10个包裹的详细时间线，避免文件过大
        var sortedParcelIds = _timelines.Keys.OrderBy(id => id).ToList();
        var parcelIdsToShow = new HashSet<long>();
        
        // 前10个
        foreach (var id in sortedParcelIds.Take(10))
        {
            parcelIdsToShow.Add(id);
        }
        
        // 后10个
        foreach (var id in sortedParcelIds.TakeLast(10))
        {
            parcelIdsToShow.Add(id);
        }
        
        // 随机抽取中间10个
        var middleIds = sortedParcelIds.Skip(10).Take(sortedParcelIds.Count - 20).ToList();
        if (middleIds.Count > 0)
        {
            var random = new Random(42); // 固定种子以便结果可重现
            var selectedMiddle = middleIds.OrderBy(_ => random.Next()).Take(10);
            foreach (var id in selectedMiddle)
            {
                parcelIdsToShow.Add(id);
            }
        }
        
        foreach (var parcelId in sortedParcelIds.Where(id => parcelIdsToShow.Contains(id)))
        {
            var snapshot = parcelLifecycleService.Get(new ParcelId(parcelId));
            if (snapshot == null) continue;
            
            sb.AppendLine($"### 包裹 #{parcelId:D6}");
            
            if (_timelines.TryGetValue(parcelId, out var timeline))
            {
                foreach (var evt in timeline)
                {
                    var timeStr = FormatElapsedTime(evt.ElapsedMs);
                    var details = !string.IsNullOrEmpty(evt.Details) ? $" - {evt.Details}" : "";
                    sb.AppendLine($"- [{timeStr}] {evt.Label}{details}");
                }
            }
            
            // 添加最终状态总结
            sb.AppendLine($"- **最终状态**: {GetOutcomeDescription(snapshot)}");
            sb.AppendLine();
        }
        
        if (sortedParcelIds.Count > parcelIdsToShow.Count)
        {
            sb.AppendLine($"_（注：为控制报告大小，仅显示部分包裹详情。共 {sortedParcelIds.Count} 个包裹。）_");
            sb.AppendLine();
        }
        
        return sb.ToString();
    }

    private static string FormatElapsedTime(double elapsedMs)
    {
        var ts = TimeSpan.FromMilliseconds(elapsedMs);
        return $"{ts.Minutes:D2}:{ts.Seconds:D2}.{ts.Milliseconds:D3}";
    }

    private static string GetOutcomeDescription(ParcelSnapshot snapshot)
    {
        return snapshot.SortingOutcome switch
        {
            ParcelSortingOutcome.NormalSort => $"正常落格到格口 {snapshot.ActualChuteId?.Value}",
            ParcelSortingOutcome.ForceEject => $"强排到异常口 {snapshot.ActualChuteId?.Value}（原因: {snapshot.DiscardReason}）",
            ParcelSortingOutcome.Missort => $"误分到格口 {snapshot.ActualChuteId?.Value}",
            ParcelSortingOutcome.Unprocessed => "未处理",
            _ => "未知状态"
        };
    }
}

/// <summary>
/// 时间线事件。
/// </summary>
public class TimelineEvent
{
    /// <summary>
    /// 时间戳。
    /// </summary>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// 相对仿真开始的毫秒数。
    /// </summary>
    public double ElapsedMs { get; init; }

    /// <summary>
    /// 事件标签（如 "Created", "Routed", "LoadPlanned"）。
    /// </summary>
    public required string Label { get; init; }

    /// <summary>
    /// 事件详情。
    /// </summary>
    public string? Details { get; init; }
}

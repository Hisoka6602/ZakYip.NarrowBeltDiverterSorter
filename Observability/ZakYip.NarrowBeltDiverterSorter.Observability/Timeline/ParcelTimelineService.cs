using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Parcels;

namespace ZakYip.NarrowBeltDiverterSorter.Observability.Timeline;

/// <summary>
/// 包裹生命周期时间线服务实现
/// 使用环形缓冲区存储，避免无限增长
/// </summary>
public sealed class ParcelTimelineService : IParcelTimelineService
{
    private readonly ILogger<ParcelTimelineService> _logger;
    private readonly int _capacity;
    private readonly ConcurrentQueue<ParcelTimelineEventArgs> _buffer;
    private int _count;

    /// <summary>
    /// 初始化包裹时间线服务
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="capacity">缓冲区容量（默认 10000）</param>
    public ParcelTimelineService(ILogger<ParcelTimelineService> logger, int capacity = 10000)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _capacity = capacity > 0 ? capacity : throw new ArgumentOutOfRangeException(nameof(capacity), "容量必须大于 0");
        _buffer = new ConcurrentQueue<ParcelTimelineEventArgs>();
        _count = 0;
    }

    /// <inheritdoc/>
    public void Append(ParcelTimelineEventArgs eventArgs)
    {
        try
        {
            _buffer.Enqueue(eventArgs);
            Interlocked.Increment(ref _count);

            // 如果超过容量限制，移除最旧的事件
            while (_count > _capacity)
            {
                if (_buffer.TryDequeue(out _))
                {
                    Interlocked.Decrement(ref _count);
                }
                else
                {
                    break;
                }
            }

            _logger.LogTrace(
                "包裹时间线事件已添加: ParcelId={ParcelId}, EventType={EventType}, OccurredAt={OccurredAt}",
                eventArgs.ParcelId,
                eventArgs.EventType,
                eventArgs.OccurredAt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加包裹时间线事件失败: ParcelId={ParcelId}, EventType={EventType}",
                eventArgs.ParcelId,
                eventArgs.EventType);
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<ParcelTimelineEventArgs> QueryByParcel(long parcelId, int maxCount = 100)
    {
        try
        {
            var events = _buffer
                .Where(e => e.ParcelId == parcelId)
                .OrderBy(e => e.OccurredAt)
                .Take(Math.Max(1, maxCount))
                .ToList();

            _logger.LogDebug(
                "查询包裹时间线: ParcelId={ParcelId}, EventCount={EventCount}, MaxCount={MaxCount}",
                parcelId,
                events.Count,
                maxCount);

            return events;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询包裹时间线失败: ParcelId={ParcelId}", parcelId);
            return Array.Empty<ParcelTimelineEventArgs>();
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<ParcelTimelineEventArgs> QueryRecent(int maxCount = 100)
    {
        try
        {
            var events = _buffer
                .OrderByDescending(e => e.OccurredAt)
                .Take(Math.Max(1, maxCount))
                .ToList();

            _logger.LogDebug(
                "查询最近时间线事件: EventCount={EventCount}, MaxCount={MaxCount}",
                events.Count,
                maxCount);

            return events;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询最近时间线事件失败");
            return Array.Empty<ParcelTimelineEventArgs>();
        }
    }
}

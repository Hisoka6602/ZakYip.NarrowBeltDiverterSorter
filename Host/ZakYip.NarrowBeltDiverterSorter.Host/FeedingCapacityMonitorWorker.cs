using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Feeding;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Parcels;
using ZakYip.NarrowBeltDiverterSorter.Observability.LiveView;

namespace ZakYip.NarrowBeltDiverterSorter.Host;

/// <summary>
/// 供包容量监控工作器
/// 定期更新 LiveView 中的供包容量快照
/// </summary>
public class FeedingCapacityMonitorWorker : BackgroundService
{
    private readonly IFeedingCapacityOptionsRepository _capacityRepo;
    private readonly IParcelLifecycleTracker _lifecycleTracker;
    private readonly IFeedingBackpressureController? _backpressureController;
    private readonly NarrowBeltLiveView _liveView;
    private readonly ILogger<FeedingCapacityMonitorWorker> _logger;
    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    public FeedingCapacityMonitorWorker(
        IFeedingCapacityOptionsRepository capacityRepo,
        IParcelLifecycleTracker lifecycleTracker,
        INarrowBeltLiveView liveView,
        ILogger<FeedingCapacityMonitorWorker> logger,
        IFeedingBackpressureController? backpressureController = null)
    {
        _capacityRepo = capacityRepo ?? throw new ArgumentNullException(nameof(capacityRepo));
        _lifecycleTracker = lifecycleTracker ?? throw new ArgumentNullException(nameof(lifecycleTracker));
        _liveView = (liveView as NarrowBeltLiveView) ?? throw new ArgumentException("LiveView must be NarrowBeltLiveView", nameof(liveView));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _backpressureController = backpressureController;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("供包容量监控工作器已启动");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await UpdateFeedingCapacitySnapshotAsync(stoppingToken);
                await Task.Delay(UpdateInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // 正常停止
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新供包容量快照时发生错误");
                await Task.Delay(UpdateInterval, stoppingToken);
            }
        }

        _logger.LogInformation("供包容量监控工作器已停止");
    }

    private async Task UpdateFeedingCapacitySnapshotAsync(CancellationToken cancellationToken)
    {
        try
        {
            var config = await _capacityRepo.LoadAsync(cancellationToken);
            var inFlightCount = _lifecycleTracker.GetInFlightCount();
            var upstreamPendingCount = _lifecycleTracker.GetUpstreamPendingCount();

            var snapshot = new FeedingCapacitySnapshot
            {
                CurrentInFlightParcels = inFlightCount,
                MaxInFlightParcels = config.MaxInFlightParcels,
                CurrentUpstreamPendingRequests = upstreamPendingCount,
                MaxUpstreamPendingRequests = config.MaxUpstreamPendingRequests,
                FeedingThrottledCount = _backpressureController?.GetThrottleCount() ?? 0,
                FeedingPausedCount = _backpressureController?.GetPauseCount() ?? 0,
                ThrottleMode = config.ThrottleMode.ToString(),
                LastUpdatedAt = DateTimeOffset.UtcNow
            };

            _liveView.UpdateFeedingCapacity(snapshot);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "加载供包容量配置失败");
        }
    }
}

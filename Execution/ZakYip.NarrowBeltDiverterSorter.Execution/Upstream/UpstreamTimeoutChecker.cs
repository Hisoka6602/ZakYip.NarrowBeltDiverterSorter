using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Parcels;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Upstream;

namespace ZakYip.NarrowBeltDiverterSorter.Execution.Upstream;

/// <summary>
/// 上游请求超时检查服务
/// 周期性检查待处理请求，将超时的包裹分配到异常格口
/// </summary>
public class UpstreamTimeoutChecker
{
    private readonly IUpstreamRequestTracker _requestTracker;
    private readonly IUpstreamRoutingConfigProvider _configProvider;
    private readonly IParcelLifecycleService _parcelLifecycleService;
    private readonly IParcelLifecycleTracker _lifecycleTracker;
    private readonly IEventBus _eventBus;
    private readonly ILogger<UpstreamTimeoutChecker> _logger;

    public UpstreamTimeoutChecker(
        IUpstreamRequestTracker requestTracker,
        IUpstreamRoutingConfigProvider configProvider,
        IParcelLifecycleService parcelLifecycleService,
        IParcelLifecycleTracker lifecycleTracker,
        IEventBus eventBus,
        ILogger<UpstreamTimeoutChecker> logger)
    {
        _requestTracker = requestTracker ?? throw new ArgumentNullException(nameof(requestTracker));
        _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
        _parcelLifecycleService = parcelLifecycleService ?? throw new ArgumentNullException(nameof(parcelLifecycleService));
        _lifecycleTracker = lifecycleTracker ?? throw new ArgumentNullException(nameof(lifecycleTracker));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 检查并处理超时的请求
    /// 此方法应该被周期性调用（例如每秒一次）
    /// </summary>
    public void CheckTimeouts()
    {
        try
        {
            var currentTime = DateTimeOffset.Now;
            var timedOutRequests = _requestTracker.GetTimedOutRequests(currentTime);

            if (timedOutRequests.Count == 0)
            {
                return; // 没有超时的请求
            }

            _logger.LogWarning("发现 {Count} 个超时的上游请求", timedOutRequests.Count);

            var config = _configProvider.GetCurrentOptions();
            var errorChuteId = new ChuteId(config.ErrorChuteId);

            foreach (var request in timedOutRequests)
            {
                try
                {
                    // 标记为超时
                    _requestTracker.MarkTimedOut(request.ParcelId, currentTime);

                    // 绑定到异常格口
                    _parcelLifecycleService.BindChuteId(request.ParcelId, errorChuteId);

                    _logger.LogWarning(
                        "包裹 {ParcelId} 上游分配超时，已分配到异常格口 {ErrorChuteId}。Deadline={Deadline}, CurrentTime={CurrentTime}",
                        request.ParcelId.Value,
                        errorChuteId.Value,
                        request.Deadline,
                        currentTime);

                    // 更新生命周期状态
                    _lifecycleTracker.UpdateStatus(
                        request.ParcelId,
                        ParcelStatus.DivertedToException,
                        ParcelFailureReason.WaitingUpstreamResultTimeout,
                        $"等待上游分配结果超时，已分配到异常格口 {errorChuteId.Value}");

                    // 发布事件到EventBus（用于可观测性）
                    var routedEvent = new Observability.Events.ParcelRoutedEventArgs
                    {
                        ParcelId = request.ParcelId.Value,
                        ChuteId = errorChuteId.Value,
                        IsSuccess = false,
                        RoutedAt = currentTime,
                        Message = "等待上游分配结果超时"
                    };
                    _ = _eventBus.PublishAsync(routedEvent);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "处理包裹 {ParcelId} 超时时发生异常",
                        request.ParcelId.Value);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查上游请求超时时发生异常");
        }
    }
}

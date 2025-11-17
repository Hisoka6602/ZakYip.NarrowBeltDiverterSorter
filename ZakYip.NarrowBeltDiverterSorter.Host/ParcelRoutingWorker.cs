using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Feeding;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Parcels;
using ZakYip.NarrowBeltDiverterSorter.UpstreamContracts.Models;

namespace ZakYip.NarrowBeltDiverterSorter.Host;

/// <summary>
/// 包裹路由工作器
/// 订阅包裹创建事件，调用上游系统请求格口，并更新包裹状态
/// </summary>
public class ParcelRoutingWorker : BackgroundService
{
    private readonly ILogger<ParcelRoutingWorker> _logger;
    private readonly IUpstreamSortingApiClient _upstreamClient;
    private readonly IParcelLifecycleService _parcelLifecycleService;
    private readonly IParcelLifecycleTracker _lifecycleTracker;

    /// <summary>
    /// 包裹路由完成事件
    /// </summary>
    public event EventHandler<ParcelRoutedEventArgs>? ParcelRouted;

    public ParcelRoutingWorker(
        ILogger<ParcelRoutingWorker> logger,
        IUpstreamSortingApiClient upstreamClient,
        IParcelLifecycleService parcelLifecycleService,
        IParcelLifecycleTracker lifecycleTracker)
    {
        _logger = logger;
        _upstreamClient = upstreamClient;
        _parcelLifecycleService = parcelLifecycleService;
        _lifecycleTracker = lifecycleTracker;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("包裹路由工作器已启动");

        // 这里只是框架，实际需要订阅ParcelCreatedFromInfeedEventArgs
        // 在真实实现中，需要通过事件总线或其他机制来订阅入口事件
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }

        _logger.LogInformation("包裹路由工作器已停止");
    }

    /// <summary>
    /// 处理包裹创建事件
    /// 此方法应该由事件订阅机制调用
    /// </summary>
    /// <param name="eventArgs">包裹创建事件参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task HandleParcelCreatedAsync(
        ParcelCreatedFromInfeedEventArgs eventArgs,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "开始为包裹 {ParcelId} 请求格口分配",
                eventArgs.ParcelId.Value);

            // 创建包裹
            _parcelLifecycleService.CreateParcel(
                eventArgs.ParcelId,
                eventArgs.Barcode,
                eventArgs.InfeedTriggerTime);

            // 更新生命周期状态：已创建
            _lifecycleTracker.UpdateStatus(
                eventArgs.ParcelId,
                ParcelStatus.Created,
                ParcelFailureReason.None,
                "包裹从入口传感器创建");

            // 调用上游系统请求格口
            var request = new ParcelRoutingRequestDto
            {
                ParcelId = eventArgs.ParcelId.Value,
                RequestTime = DateTimeOffset.UtcNow
            };

            var response = await _upstreamClient.RequestChuteAsync(request, cancellationToken);

            // 根据响应更新包裹状态
            if (response.IsSuccess)
            {
                var chuteId = new ChuteId(response.ChuteId);
                _parcelLifecycleService.BindChuteId(eventArgs.ParcelId, chuteId);

                _logger.LogInformation(
                    "包裹 {ParcelId} 成功分配到格口 {ChuteId}",
                    eventArgs.ParcelId.Value,
                    response.ChuteId);

                // 发布路由成功事件
                ParcelRouted?.Invoke(this, new ParcelRoutedEventArgs
                {
                    ParcelId = eventArgs.ParcelId,
                    ChuteId = chuteId,
                    IsSuccess = true,
                    RoutedTime = response.ResponseTime
                });
            }
            else
            {
                // 路由失败，更新状态为失败
                _parcelLifecycleService.UpdateRouteState(
                    eventArgs.ParcelId,
                    ParcelRouteState.Failed);

                // 更新生命周期状态：失败（上游超时）
                _lifecycleTracker.UpdateStatus(
                    eventArgs.ParcelId,
                    ParcelStatus.Failed,
                    ParcelFailureReason.UpstreamTimeout,
                    $"上游返回失败: {response.ErrorMessage}");

                _logger.LogWarning(
                    "包裹 {ParcelId} 格口分配失败: {ErrorMessage}",
                    eventArgs.ParcelId.Value,
                    response.ErrorMessage);

                // 发布路由失败事件
                ParcelRouted?.Invoke(this, new ParcelRoutedEventArgs
                {
                    ParcelId = eventArgs.ParcelId,
                    ChuteId = null,
                    IsSuccess = false,
                    ErrorMessage = response.ErrorMessage,
                    RoutedTime = response.ResponseTime
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "处理包裹 {ParcelId} 路由请求时发生异常",
                eventArgs.ParcelId.Value);

            // 更新状态为失败
            try
            {
                _parcelLifecycleService.UpdateRouteState(
                    eventArgs.ParcelId,
                    ParcelRouteState.Failed);

                // 更新生命周期状态：失败（未知原因）
                _lifecycleTracker.UpdateStatus(
                    eventArgs.ParcelId,
                    ParcelStatus.Failed,
                    ParcelFailureReason.Unknown,
                    $"处理路由请求时发生异常: {ex.Message}");
            }
            catch
            {
                // 忽略更新状态失败的异常
            }

            // 发布路由失败事件
            ParcelRouted?.Invoke(this, new ParcelRoutedEventArgs
            {
                ParcelId = eventArgs.ParcelId,
                ChuteId = null,
                IsSuccess = false,
                ErrorMessage = $"处理异常: {ex.Message}"
            });
        }
    }
}

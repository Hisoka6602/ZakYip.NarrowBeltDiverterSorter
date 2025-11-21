using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Core.Enums.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Parcels;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Sorting;
using ZakYip.NarrowBeltDiverterSorter.Observability.Events;

namespace ZakYip.NarrowBeltDiverterSorter.Execution.Orchestration;

/// <summary>
/// 包裹分拣编排器
/// 监听包裹创建事件，调用 RuleEngine 请求分拣，并处理分拣结果
/// </summary>
public class ParcelSortingOrchestrator : IDisposable
{
    private readonly ILogger<ParcelSortingOrchestrator> _logger;
    private readonly ISortingRuleEnginePort _ruleEnginePort;
    private readonly IParcelLifecycleService _parcelLifecycleService;
    private readonly IEventBus _eventBus;
    private readonly IParcelTimelineService _timelineService;
    private readonly ChuteId _fallbackChuteId;
    private bool _disposed;

    public ParcelSortingOrchestrator(
        ILogger<ParcelSortingOrchestrator> logger,
        ISortingRuleEnginePort ruleEnginePort,
        IParcelLifecycleService parcelLifecycleService,
        IEventBus eventBus,
        IParcelTimelineService timelineService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _ruleEnginePort = ruleEnginePort ?? throw new ArgumentNullException(nameof(ruleEnginePort));
        _parcelLifecycleService = parcelLifecycleService ?? throw new ArgumentNullException(nameof(parcelLifecycleService));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _timelineService = timelineService ?? throw new ArgumentNullException(nameof(timelineService));

        // TODO: 从配置读取降级格口ID，这里使用默认值 999
        _fallbackChuteId = new ChuteId(999);

        // 订阅包裹创建事件
        _eventBus.Subscribe<ParcelCreatedFromInfeedEventArgs>(OnParcelCreatedAsync);

        _logger.LogInformation("包裹分拣编排器已启动");
    }

    /// <summary>
    /// 处理包裹创建事件
    /// </summary>
    private Task OnParcelCreatedAsync(ParcelCreatedFromInfeedEventArgs eventArgs, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation(
                "开始处理包裹创建事件: ParcelId={ParcelId}, Barcode={Barcode}",
                eventArgs.ParcelId, eventArgs.Barcode);

            // 记录包裹创建时间线事件
            _timelineService.Append(new ParcelTimelineEventArgs
            {
                ParcelId = eventArgs.ParcelId,
                EventType = ParcelTimelineEventType.Created,
                OccurredAt = eventArgs.InfeedTriggerTime,
                Barcode = eventArgs.Barcode,
                Note = "包裹从入口传感器检测到"
            });

            // 构造分拣请求
            var sortingRequest = new SortingRequestEventArgs
            {
                ParcelId = eventArgs.ParcelId,
                Barcode = eventArgs.Barcode,
                RequestTime = eventArgs.InfeedTriggerTime
            };

            // 调用 RuleEngine 请求分拣（异步，不阻塞主线）- fire-and-forget
            _ = RequestSortingAsync(sortingRequest, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "处理包裹创建事件时发生异常: ParcelId={ParcelId}",
                eventArgs.ParcelId);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 请求分拣（异步 fire-and-forget）
    /// </summary>
    private async Task RequestSortingAsync(SortingRequestEventArgs request, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation(
                "已向上游规则引擎发送包裹创建请求: ParcelId={ParcelId}, Barcode={Barcode}",
                request.ParcelId, request.Barcode);

            // 记录上游请求发送时间线事件
            _timelineService.Append(new ParcelTimelineEventArgs
            {
                ParcelId = request.ParcelId,
                EventType = ParcelTimelineEventType.UpstreamRequestSent,
                OccurredAt = DateTimeOffset.Now,
                Barcode = request.Barcode,
                Note = "向上游规则引擎发送分拣请求"
            });

            // 调用 RuleEngine 请求分拣
            await _ruleEnginePort.RequestSortingAsync(request, ct);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning(
                "规则引擎请求被取消: ParcelId={ParcelId}",
                request.ParcelId);

            // 记录中断事件
            _timelineService.Append(new ParcelTimelineEventArgs
            {
                ParcelId = request.ParcelId,
                EventType = ParcelTimelineEventType.Aborted,
                OccurredAt = DateTimeOffset.Now,
                Barcode = request.Barcode,
                Note = "上游请求被取消"
            });

            // 使用降级策略
            ApplyFallbackStrategy(request.ParcelId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "调用规则引擎时发生异常: ParcelId={ParcelId}，将使用降级策略",
                request.ParcelId);

            // 记录中断事件
            _timelineService.Append(new ParcelTimelineEventArgs
            {
                ParcelId = request.ParcelId,
                EventType = ParcelTimelineEventType.Aborted,
                OccurredAt = DateTimeOffset.Now,
                Barcode = request.Barcode,
                Note = $"上游请求失败: {ex.Message}"
            });

            // 使用降级策略
            ApplyFallbackStrategy(request.ParcelId);
        }
    }

    /// <summary>
    /// 应用降级策略（上游不可用时）
    /// </summary>
    private void ApplyFallbackStrategy(long parcelId)
    {
        try
        {
            var parcelIdObj = new ParcelId(parcelId);

            _logger.LogWarning(
                "上游规则引擎不可用，包裹 {ParcelId} 将使用本地降级策略，发送到异常格口: {FallbackChuteId}",
                parcelId, _fallbackChuteId.Value);

            // 绑定降级格口
            _parcelLifecycleService.BindChuteId(parcelIdObj, _fallbackChuteId);

            // 发布事件通知
            _ = _eventBus.PublishAsync(new Observability.Events.ParcelRoutedEventArgs
            {
                ParcelId = parcelIdObj.Value,
                ChuteId = _fallbackChuteId.Value,
                IsSuccess = true,
                Message = "使用本地降级策略（上游不可用）",
                RoutedAt = DateTimeOffset.Now
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "应用降级策略失败: ParcelId={ParcelId}",
                parcelId);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        // 取消订阅
        _eventBus.Unsubscribe<ParcelCreatedFromInfeedEventArgs>(OnParcelCreatedAsync);

        _logger.LogInformation("包裹分拣编排器已停止");
        _disposed = true;
    }
}

using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Communication.Upstream;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Parcels;
using ZakYip.NarrowBeltDiverterSorter.Observability.Events;
using ZakYip.NarrowBeltDiverterSorter.UpstreamContracts.Models;

namespace ZakYip.NarrowBeltDiverterSorter.Execution.Orchestration;

/// <summary>
/// 分拣结果处理器
/// 监听从 RuleEngine 收到的分拣结果，更新本地包裹状态
/// </summary>
public class SortingResultHandler : IDisposable
{
    private readonly ILogger<SortingResultHandler> _logger;
    private readonly ISortingRuleEngineClient _ruleEngineClient;
    private readonly IParcelLifecycleService _parcelLifecycleService;
    private readonly IEventBus _eventBus;
    private readonly IParcelTimelineService _timelineService;
    private bool _disposed;

    public SortingResultHandler(
        ILogger<SortingResultHandler> logger,
        ISortingRuleEngineClient ruleEngineClient,
        IParcelLifecycleService parcelLifecycleService,
        IEventBus eventBus,
        IParcelTimelineService timelineService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _ruleEngineClient = ruleEngineClient ?? throw new ArgumentNullException(nameof(ruleEngineClient));
        _parcelLifecycleService = parcelLifecycleService ?? throw new ArgumentNullException(nameof(parcelLifecycleService));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _timelineService = timelineService ?? throw new ArgumentNullException(nameof(timelineService));

        // 订阅 RuleEngine 客户端的分拣结果事件
        _ruleEngineClient.SortingResultReceived += OnSortingResultReceived;

        _logger.LogInformation("分拣结果处理器已启动");
    }

    /// <summary>
    /// 处理分拣结果
    /// </summary>
    private void OnSortingResultReceived(object? sender, SortingResultMessage result)
    {
        try
        {
            _logger.LogInformation(
                "已接收到上游规则引擎分拣结果: ParcelId={ParcelId}, ChuteNumber={ChuteNumber}, CartCount={CartCount}, Success={Success}",
                result.ParcelId, result.ChuteNumber, result.CartCount, result.Success);

            var parcelId = new ParcelId(result.ParcelId);

            // 检查包裹是否存在
            var parcel = _parcelLifecycleService.Get(parcelId);
            if (parcel == null)
            {
                _logger.LogWarning(
                    "收到分拣结果但包裹不存在: ParcelId={ParcelId}",
                    result.ParcelId);
                return;
            }

            // 检查包裹状态是否仍然有效
            if (parcel.RouteState == ParcelRouteState.Sorted || 
                parcel.RouteState == ParcelRouteState.Failed)
            {
                _logger.LogWarning(
                    "收到分拣结果但包裹已完成或失败: ParcelId={ParcelId}, CurrentState={CurrentState}",
                    result.ParcelId, parcel.RouteState);
                return;
            }

            if (result.Success)
            {
                // 更新包裹格口
                var chuteId = new ChuteId(result.ChuteNumber);
                _parcelLifecycleService.BindChuteId(parcelId, chuteId);

                _logger.LogInformation(
                    "包裹 {ParcelId} 成功分配到格口 {ChuteId}（来自上游规则引擎）",
                    result.ParcelId, result.ChuteNumber);

                // 记录上游结果接收时间线事件
                _timelineService.Append(new ParcelTimelineEventArgs
                {
                    ParcelId = result.ParcelId,
                    EventType = ParcelTimelineEventType.UpstreamResultReceived,
                    OccurredAt = result.ResultTime,
                    ChuteId = result.ChuteNumber,
                    Note = $"上游分配成功，CartCount={result.CartCount}, 处理时间={result.ProcessingTimeMs}ms"
                });

                // 记录分拣计划创建事件
                _timelineService.Append(new ParcelTimelineEventArgs
                {
                    ParcelId = result.ParcelId,
                    EventType = ParcelTimelineEventType.PlanCreated,
                    OccurredAt = DateTimeOffset.UtcNow,
                    ChuteId = result.ChuteNumber,
                    Note = $"分拣计划已创建，目标格口={result.ChuteNumber}"
                });

                // 发布路由成功事件到事件总线
                _ = _eventBus.PublishAsync(new SortingResultReceivedEventArgs
                {
                    ParcelId = result.ParcelId,
                    ChuteNumber = result.ChuteNumber,
                    CartNumber = result.CartNumber,
                    CartCount = result.CartCount,
                    Success = true,
                    ProcessingTimeMs = result.ProcessingTimeMs,
                    ResultTime = result.ResultTime
                });

                // 同时发布路由事件（兼容旧代码）
                _ = _eventBus.PublishAsync(new Observability.Events.ParcelRoutedEventArgs
                {
                    ParcelId = parcelId.Value,
                    ChuteId = chuteId.Value,
                    IsSuccess = true,
                    Message = $"上游规则引擎分配（CartCount={result.CartCount}）",
                    RoutedAt = result.ResultTime
                });
            }
            else
            {
                // 分拣失败，更新状态
                _parcelLifecycleService.UpdateRouteState(parcelId, ParcelRouteState.Failed);

                _logger.LogWarning(
                    "上游规则引擎返回失败: ParcelId={ParcelId}, Reason={Reason}",
                    result.ParcelId, result.FailureReason);

                // 记录上游结果接收时间线事件（失败）
                _timelineService.Append(new ParcelTimelineEventArgs
                {
                    ParcelId = result.ParcelId,
                    EventType = ParcelTimelineEventType.UpstreamResultReceived,
                    OccurredAt = result.ResultTime,
                    Note = $"上游分配失败: {result.FailureReason}"
                });

                // 记录中断事件
                _timelineService.Append(new ParcelTimelineEventArgs
                {
                    ParcelId = result.ParcelId,
                    EventType = ParcelTimelineEventType.Aborted,
                    OccurredAt = DateTimeOffset.UtcNow,
                    Note = $"上游分拣失败: {result.FailureReason}"
                });

                // 发布失败事件
                _ = _eventBus.PublishAsync(new SortingResultReceivedEventArgs
                {
                    ParcelId = result.ParcelId,
                    ChuteNumber = result.ChuteNumber,
                    CartNumber = result.CartNumber,
                    CartCount = result.CartCount,
                    Success = false,
                    FailureReason = result.FailureReason,
                    ProcessingTimeMs = result.ProcessingTimeMs,
                    ResultTime = result.ResultTime
                });

                _ = _eventBus.PublishAsync(new Observability.Events.ParcelRoutedEventArgs
                {
                    ParcelId = parcelId.Value,
                    ChuteId = 0, // 使用 0 表示没有格口
                    IsSuccess = false,
                    Message = result.FailureReason,
                    RoutedAt = result.ResultTime
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "处理分拣结果时发生异常: ParcelId={ParcelId}",
                result.ParcelId);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        // 取消订阅
        _ruleEngineClient.SortingResultReceived -= OnSortingResultReceived;

        _logger.LogInformation("分拣结果处理器已停止");
        _disposed = true;
    }
}

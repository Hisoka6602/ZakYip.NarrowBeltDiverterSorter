using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Communication.Upstream;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Parcels;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Upstream;
using ZakYip.NarrowBeltDiverterSorter.UpstreamContracts.Models;
using ZakYip.NarrowBeltDiverterSorter.Core.Enums;

namespace ZakYip.NarrowBeltDiverterSorter.Execution.Upstream;

/// <summary>
/// 上游响应处理服务
/// 负责接收并处理上游规则引擎的格口分配推送
/// </summary>
public class UpstreamResponseHandler : IDisposable
{
    private readonly ISortingRuleEngineClient _ruleEngineClient;
    private readonly IUpstreamRequestTracker _requestTracker;
    private readonly IParcelLifecycleService _parcelLifecycleService;
    private readonly IParcelLifecycleTracker _lifecycleTracker;
    private readonly IEventBus _eventBus;
    private readonly ILogger<UpstreamResponseHandler> _logger;
    private bool _disposed;

    public UpstreamResponseHandler(
        ISortingRuleEngineClient ruleEngineClient,
        IUpstreamRequestTracker requestTracker,
        IParcelLifecycleService parcelLifecycleService,
        IParcelLifecycleTracker lifecycleTracker,
        IEventBus eventBus,
        ILogger<UpstreamResponseHandler> logger)
    {
        _ruleEngineClient = ruleEngineClient ?? throw new ArgumentNullException(nameof(ruleEngineClient));
        _requestTracker = requestTracker ?? throw new ArgumentNullException(nameof(requestTracker));
        _parcelLifecycleService = parcelLifecycleService ?? throw new ArgumentNullException(nameof(parcelLifecycleService));
        _lifecycleTracker = lifecycleTracker ?? throw new ArgumentNullException(nameof(lifecycleTracker));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // 订阅格口分配推送事件
        _ruleEngineClient.ChuteAssignmentReceived += OnChuteAssignmentReceived;
    }

    /// <summary>
    /// 处理格口分配推送
    /// </summary>
    private void OnChuteAssignmentReceived(object? sender, ChuteAssignmentNotificationEventArgs e)
    {
        try
        {
            var parcelId = new ParcelId(e.ParcelId);
            var chuteId = new ChuteId(e.ChuteId);
            var respondedAt = e.NotificationTime;

            _logger.LogInformation(
                "收到上游格口分配推送: ParcelId={ParcelId}, ChuteId={ChuteId}",
                parcelId.Value,
                chuteId.Value);

            // 尝试标记为已分配
            bool success = _requestTracker.MarkAssigned(parcelId, chuteId, respondedAt);

            if (success)
            {
                // 成功：在Deadline内收到响应，绑定格口
                _parcelLifecycleService.BindChuteId(parcelId, chuteId);

                _logger.LogInformation(
                    "包裹 {ParcelId} 成功绑定格口 {ChuteId}",
                    parcelId.Value,
                    chuteId.Value);

                // 更新生命周期状态
                _lifecycleTracker.UpdateStatus(
                    parcelId,
                    ParcelStatus.OnMainline,
                    ParcelFailureReason.None,
                    $"已绑定格口 {chuteId.Value}");

                // 发布事件到EventBus
                var routedEvent = new Observability.Events.ParcelRoutedEventArgs
                {
                    ParcelId = parcelId.Value,
                    ChuteId = chuteId.Value,
                    IsSuccess = true,
                    RoutedAt = respondedAt
                };
                _ = _eventBus.PublishAsync(routedEvent);
            }
            else
            {
                // 失败：迟到的响应（已超时）或请求不存在
                var record = _requestTracker.GetRecord(parcelId);
                if (record == null)
                {
                    _logger.LogWarning(
                        "收到未知包裹的格口分配推送: ParcelId={ParcelId}",
                        parcelId.Value);
                }
                else if (record.Status == UpstreamRequestStatus.TimedOut)
                {
                    _logger.LogWarning(
                        "收到迟到的格口分配推送（已超时）: ParcelId={ParcelId}, ChuteId={ChuteId}, Deadline={Deadline}, RespondedAt={RespondedAt}",
                        parcelId.Value,
                        chuteId.Value,
                        record.Deadline,
                        respondedAt);
                }
                else if (record.Status == UpstreamRequestStatus.Assigned)
                {
                    _logger.LogWarning(
                        "收到重复的格口分配推送（已分配）: ParcelId={ParcelId}, ExistingChuteId={ExistingChuteId}, NewChuteId={NewChuteId}",
                        parcelId.Value,
                        record.AssignedChuteId?.Value,
                        chuteId.Value);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理格口分配推送时发生异常");
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        // 取消订阅
        _ruleEngineClient.ChuteAssignmentReceived -= OnChuteAssignmentReceived;

        _disposed = true;
    }
}

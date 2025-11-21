using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.SystemState;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Upstream;
using ZakYip.NarrowBeltDiverterSorter.Observability.Events;
using ZakYip.NarrowBeltDiverterSorter.Core.Enums;
using ZakYip.NarrowBeltDiverterSorter.Core.Enums.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Enums.Observability;

namespace ZakYip.NarrowBeltDiverterSorter.Observability.LiveView;

/// <summary>
/// 窄带分拣机实时视图实现
/// 订阅领域事件并维护内存快照
/// </summary>
public class NarrowBeltLiveView : INarrowBeltLiveView, IDisposable
{
    private readonly IEventBus _eventBus;
    private readonly ISystemFaultService _faultService;
    private readonly ILogger<NarrowBeltLiveView> _logger;
    private readonly object _lock = new();

    // 内存快照
    private LineSpeedSnapshot _lineSpeedSnapshot = new()
    {
        ActualMmps = 0,
        TargetMmps = 0,
        Status = LineSpeedStatus.Unknown,
        LastUpdatedAt = DateTimeOffset.Now
    };

    private OriginCartSnapshot _originCartSnapshot = new()
    {
        CartId = null,
        LastUpdatedAt = DateTimeOffset.Now
    };

    private ChuteCartSnapshot _chuteCartSnapshot = new()
    {
        Mapping = new Dictionary<long, long?>(),
        LastUpdatedAt = DateTimeOffset.Now
    };

    private ParcelSummary? _lastCreatedParcel;
    private ParcelSummary? _lastDivertedParcel;
    private readonly ConcurrentDictionary<long, ParcelSummary> _onlineParcels = new();

    private DeviceStatusSnapshot _deviceStatusSnapshot = new()
    {
        Status = DeviceStatus.Idle,
        Message = null,
        LastUpdatedAt = DateTimeOffset.Now
    };

    private CartLayoutSnapshot _cartLayoutSnapshot = new()
    {
        CartPositions = Array.Empty<CartPositionSnapshot>(),
        LastUpdatedAt = DateTimeOffset.Now
    };

    private LineRunStateSnapshot _lineRunStateSnapshot = new()
    {
        State = "Idle",
        Message = null,
        LastUpdatedAt = DateTimeOffset.Now
    };

    private SafetyStateSnapshot _safetyStateSnapshot = new()
    {
        State = "Safe",
        Source = null,
        Message = null,
        LastUpdatedAt = DateTimeOffset.Now
    };

    private UpstreamRuleEngineSnapshot _upstreamRuleEngineSnapshot = new()
    {
        Mode = "Disabled",
        Status = UpstreamConnectionStatus.Disabled,
        ConnectionAddress = null,
        LastUpdatedAt = DateTimeOffset.Now
    };

    private LastSortingRequestSnapshot? _lastSortingRequest;
    private LastSortingResultSnapshot? _lastSortingResult;

    private FeedingCapacitySnapshot _feedingCapacitySnapshot = new()
    {
        CurrentInFlightParcels = 0,
        MaxInFlightParcels = 200,
        CurrentUpstreamPendingRequests = 0,
        MaxUpstreamPendingRequests = 10,
        FeedingThrottledCount = 0,
        FeedingPausedCount = 0,
        ThrottleMode = "None",
        LastUpdatedAt = DateTimeOffset.Now
    };

    public NarrowBeltLiveView(IEventBus eventBus, ISystemFaultService faultService, ILogger<NarrowBeltLiveView> logger)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _faultService = faultService ?? throw new ArgumentNullException(nameof(faultService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // 订阅事件
        SubscribeToEvents();

        _logger.LogInformation("实时视图聚合器已初始化");
    }

    private void SubscribeToEvents()
    {
        _eventBus.Subscribe<LineSpeedChangedEventArgs>(OnLineSpeedChangedAsync);
        _eventBus.Subscribe<CartAtChuteChangedEventArgs>(OnCartAtChuteChangedAsync);
        _eventBus.Subscribe<OriginCartChangedEventArgs>(OnOriginCartChangedAsync);
        _eventBus.Subscribe<ParcelCreatedEventArgs>(OnParcelCreatedAsync);
        _eventBus.Subscribe<ParcelDivertedEventArgs>(OnParcelDivertedAsync);
        _eventBus.Subscribe<DeviceStatusChangedEventArgs>(OnDeviceStatusChangedAsync);
        _eventBus.Subscribe<CartLayoutChangedEventArgs>(OnCartLayoutChangedAsync);
        _eventBus.Subscribe<LineRunStateChangedEventArgs>(OnLineRunStateChangedAsync);
        _eventBus.Subscribe<SafetyStateChangedEventArgs>(OnSafetyStateChangedAsync);
        _eventBus.Subscribe<UpstreamRuleEngineStatusChangedEventArgs>(OnUpstreamRuleEngineStatusChangedAsync);
        _eventBus.Subscribe<UpstreamMetricsEventArgs>(OnUpstreamMetricsAsync);
        _eventBus.Subscribe<ParcelCreatedFromInfeedEventArgs>(OnParcelCreatedFromInfeedAsync);
        _eventBus.Subscribe<SortingResultReceivedEventArgs>(OnSortingResultReceivedAsync);

        _logger.LogDebug("已订阅所有实时监控事件");
    }

    private Task OnLineSpeedChangedAsync(LineSpeedChangedEventArgs eventArgs, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            _lineSpeedSnapshot = new LineSpeedSnapshot
            {
                ActualMmps = eventArgs.ActualMmps,
                TargetMmps = eventArgs.TargetMmps,
                Status = eventArgs.Status,
                LastUpdatedAt = eventArgs.OccurredAt
            };
        }

        _logger.LogTrace("主线速度快照已更新: {ActualMmps} mm/s, 状态: {Status}", 
            eventArgs.ActualMmps, eventArgs.Status);

        return Task.CompletedTask;
    }

    private Task OnCartAtChuteChangedAsync(CartAtChuteChangedEventArgs eventArgs, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            var mapping = new Dictionary<long, long?>(_chuteCartSnapshot.Mapping)
            {
                [eventArgs.ChuteId] = eventArgs.CartId
            };

            _chuteCartSnapshot = new ChuteCartSnapshot
            {
                Mapping = mapping,
                LastUpdatedAt = eventArgs.OccurredAt
            };
        }

        _logger.LogTrace("格口小车映射已更新: 格口 {ChuteId} -> 小车 {CartId}", 
            eventArgs.ChuteId, eventArgs.CartId);

        return Task.CompletedTask;
    }

    private Task OnOriginCartChangedAsync(OriginCartChangedEventArgs eventArgs, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            _originCartSnapshot = new OriginCartSnapshot
            {
                CartId = eventArgs.CartId,
                LastUpdatedAt = eventArgs.OccurredAt
            };
        }

        _logger.LogTrace("原点小车快照已更新: {CartId}", eventArgs.CartId);

        return Task.CompletedTask;
    }

    private Task OnParcelCreatedAsync(ParcelCreatedEventArgs eventArgs, CancellationToken cancellationToken)
    {
        var summary = new ParcelSummary
        {
            ParcelId = eventArgs.ParcelId,
            Barcode = eventArgs.Barcode,
            WeightKg = eventArgs.WeightKg,
            VolumeCubicMm = eventArgs.VolumeCubicMm,
            TargetChuteId = eventArgs.TargetChuteId,
            CreatedAt = eventArgs.CreatedAt
        };

        lock (_lock)
        {
            _lastCreatedParcel = summary;
            _onlineParcels[eventArgs.ParcelId] = summary;
        }

        _logger.LogTrace("包裹创建快照已更新: {ParcelId}, 条码: {Barcode}", 
            eventArgs.ParcelId, eventArgs.Barcode);

        return Task.CompletedTask;
    }

    private Task OnParcelDivertedAsync(ParcelDivertedEventArgs eventArgs, CancellationToken cancellationToken)
    {
        var summary = new ParcelSummary
        {
            ParcelId = eventArgs.ParcelId,
            Barcode = eventArgs.Barcode,
            WeightKg = eventArgs.WeightKg,
            VolumeCubicMm = eventArgs.VolumeCubicMm,
            TargetChuteId = eventArgs.TargetChuteId,
            ActualChuteId = eventArgs.ActualChuteId,
            CreatedAt = DateTimeOffset.Now, // 没有创建时间，使用当前本地时间
            DivertedAt = eventArgs.DivertedAt
        };

        lock (_lock)
        {
            _lastDivertedParcel = summary;
            // 从在线包裹列表中移除
            _onlineParcels.TryRemove(eventArgs.ParcelId, out _);
        }

        _logger.LogTrace("包裹落格快照已更新: {ParcelId}, 目标格口: {TargetChuteId}, 实际格口: {ActualChuteId}", 
            eventArgs.ParcelId, eventArgs.TargetChuteId, eventArgs.ActualChuteId);

        return Task.CompletedTask;
    }

    private Task OnDeviceStatusChangedAsync(DeviceStatusChangedEventArgs eventArgs, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            _deviceStatusSnapshot = new DeviceStatusSnapshot
            {
                Status = eventArgs.Status,
                Message = eventArgs.Message,
                LastUpdatedAt = eventArgs.OccurredAt
            };
        }

        _logger.LogTrace("设备状态快照已更新: {Status}, 消息: {Message}", 
            eventArgs.Status, eventArgs.Message);

        return Task.CompletedTask;
    }

    private Task OnCartLayoutChangedAsync(CartLayoutChangedEventArgs eventArgs, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            _cartLayoutSnapshot = new CartLayoutSnapshot
            {
                CartPositions = eventArgs.CartPositions,
                LastUpdatedAt = eventArgs.OccurredAt
            };

            // 同时更新格口小车映射
            _chuteCartSnapshot = new ChuteCartSnapshot
            {
                Mapping = eventArgs.ChuteToCartMapping,
                LastUpdatedAt = eventArgs.OccurredAt
            };
        }

        _logger.LogTrace("小车布局快照已更新: {CartCount} 辆小车", eventArgs.CartPositions.Count);

        return Task.CompletedTask;
    }

    private Task OnLineRunStateChangedAsync(LineRunStateChangedEventArgs eventArgs, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            _lineRunStateSnapshot = new LineRunStateSnapshot
            {
                State = eventArgs.State,
                Message = eventArgs.Message,
                LastUpdatedAt = eventArgs.OccurredAt
            };
        }

        _logger.LogInformation("线体运行状态快照已更新: {State}, 消息: {Message}", 
            eventArgs.State, eventArgs.Message);

        return Task.CompletedTask;
    }

    private Task OnSafetyStateChangedAsync(SafetyStateChangedEventArgs eventArgs, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            _safetyStateSnapshot = new SafetyStateSnapshot
            {
                State = eventArgs.State,
                Source = eventArgs.Source,
                Message = eventArgs.Message,
                LastUpdatedAt = eventArgs.OccurredAt
            };
        }

        _logger.LogWarning("安全状态快照已更新: {State}, 源: {Source}, 消息: {Message}", 
            eventArgs.State, eventArgs.Source, eventArgs.Message);

        return Task.CompletedTask;
    }

    private Task OnUpstreamRuleEngineStatusChangedAsync(UpstreamRuleEngineStatusChangedEventArgs eventArgs, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            _upstreamRuleEngineSnapshot = _upstreamRuleEngineSnapshot with
            {
                Mode = eventArgs.Mode,
                Status = eventArgs.Status,
                ConnectionAddress = eventArgs.ConnectionAddress,
                LastUpdatedAt = eventArgs.Timestamp
            };
        }

        _logger.LogInformation("上游规则引擎状态快照已更新: Mode={Mode}, Status={Status}", 
            eventArgs.Mode, eventArgs.Status);

        return Task.CompletedTask;
    }

    private Task OnUpstreamMetricsAsync(UpstreamMetricsEventArgs eventArgs, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            _upstreamRuleEngineSnapshot = _upstreamRuleEngineSnapshot with
            {
                TotalRequests = eventArgs.TotalRequests,
                SuccessfulResponses = eventArgs.SuccessfulResponses,
                FailedResponses = eventArgs.FailedResponses,
                AverageLatencyMs = eventArgs.AverageLatencyMs,
                LastError = eventArgs.LastError,
                LastErrorAt = eventArgs.LastErrorAt,
                LastUpdatedAt = eventArgs.Timestamp
            };
        }

        _logger.LogDebug("上游规则引擎指标已更新: Total={Total}, Success={Success}, Failed={Failed}, AvgLatency={AvgLatency}ms",
            eventArgs.TotalRequests, eventArgs.SuccessfulResponses, eventArgs.FailedResponses, eventArgs.AverageLatencyMs);

        return Task.CompletedTask;
    }

    public LineSpeedSnapshot GetLineSpeed()
    {
        lock (_lock)
        {
            return _lineSpeedSnapshot;
        }
    }

    public ParcelSummary? GetLastCreatedParcel()
    {
        lock (_lock)
        {
            return _lastCreatedParcel;
        }
    }

    public ParcelSummary? GetLastDivertedParcel()
    {
        lock (_lock)
        {
            return _lastDivertedParcel;
        }
    }

    public IReadOnlyCollection<ParcelSummary> GetOnlineParcels()
    {
        return _onlineParcels.Values.ToList();
    }

    public DeviceStatusSnapshot GetDeviceStatus()
    {
        lock (_lock)
        {
            return _deviceStatusSnapshot;
        }
    }

    public OriginCartSnapshot GetOriginCart()
    {
        lock (_lock)
        {
            return _originCartSnapshot;
        }
    }

    public ChuteCartSnapshot GetChuteCarts()
    {
        lock (_lock)
        {
            return _chuteCartSnapshot;
        }
    }

    public long? GetChuteCart(long chuteId)
    {
        lock (_lock)
        {
            return _chuteCartSnapshot.Mapping.TryGetValue(chuteId, out var cartId) ? cartId : null;
        }
    }

    public CartLayoutSnapshot GetCartLayout()
    {
        lock (_lock)
        {
            return _cartLayoutSnapshot;
        }
    }

    public LineRunStateSnapshot GetLineRunState()
    {
        lock (_lock)
        {
            return _lineRunStateSnapshot;
        }
    }

    public SafetyStateSnapshot GetSafetyState()
    {
        lock (_lock)
        {
            return _safetyStateSnapshot;
        }
    }

    public UpstreamRuleEngineSnapshot GetUpstreamRuleEngineStatus()
    {
        lock (_lock)
        {
            return _upstreamRuleEngineSnapshot;
        }
    }

    public LastSortingRequestSnapshot? GetLastSortingRequest()
    {
        lock (_lock)
        {
            return _lastSortingRequest;
        }
    }

    public LastSortingResultSnapshot? GetLastSortingResult()
    {
        lock (_lock)
        {
            return _lastSortingResult;
        }
    }

    private Task OnParcelCreatedFromInfeedAsync(ParcelCreatedFromInfeedEventArgs eventArgs, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            _lastSortingRequest = new LastSortingRequestSnapshot
            {
                ParcelId = eventArgs.ParcelId,
                Barcode = eventArgs.Barcode,
                CartNumber = null,
                RequestTime = eventArgs.InfeedTriggerTime
            };
        }

        _logger.LogTrace("最后分拣请求快照已更新: ParcelId={ParcelId}", eventArgs.ParcelId);
        return Task.CompletedTask;
    }

    private Task OnSortingResultReceivedAsync(SortingResultReceivedEventArgs eventArgs, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            _lastSortingResult = new LastSortingResultSnapshot
            {
                ParcelId = eventArgs.ParcelId,
                ChuteNumber = eventArgs.ChuteNumber,
                CartCount = eventArgs.CartCount,
                Success = eventArgs.Success,
                FailureReason = eventArgs.FailureReason,
                ResultTime = eventArgs.ResultTime
            };
        }

        _logger.LogTrace("最后分拣结果快照已更新: ParcelId={ParcelId}, Success={Success}", 
            eventArgs.ParcelId, eventArgs.Success);
        return Task.CompletedTask;
    }

    public FeedingCapacitySnapshot GetFeedingCapacity()
    {
        lock (_lock)
        {
            return _feedingCapacitySnapshot;
        }
    }

    /// <summary>
    /// 更新供包容量快照（由外部服务定期调用）
    /// </summary>
    public void UpdateFeedingCapacity(FeedingCapacitySnapshot snapshot)
    {
        lock (_lock)
        {
            _feedingCapacitySnapshot = snapshot;
        }
    }

    public SystemFaultsStateSnapshot GetSystemFaultsState()
    {
        var faults = _faultService.GetActiveFaults();
        var hasBlockingFault = _faultService.HasBlockingFault();

        return new SystemFaultsStateSnapshot
        {
            CurrentFaults = faults.Select(f => new SystemFaultSnapshot
            {
                FaultCode = f.FaultCode.ToString(),
                Message = f.Message,
                OccurredAt = f.OccurredAt,
                IsBlocking = f.IsBlocking
            }).ToList(),
            HasBlockingFault = hasBlockingFault,
            LastUpdatedAt = DateTimeOffset.Now
        };
    }

    public void Dispose()
    {
        _logger.LogInformation("实时视图聚合器正在释放资源...");
        
        // 取消订阅事件
        // Note: EventBus 不支持取消订阅，这里只记录日志
        
        _logger.LogInformation("实时视图聚合器已释放");
    }
}

using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Observability.Events;

namespace ZakYip.NarrowBeltDiverterSorter.Observability.LiveView;

/// <summary>
/// 窄带分拣机实时视图实现
/// 订阅领域事件并维护内存快照
/// </summary>
public class NarrowBeltLiveView : INarrowBeltLiveView, IDisposable
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<NarrowBeltLiveView> _logger;
    private readonly object _lock = new();

    // 内存快照
    private LineSpeedSnapshot _lineSpeedSnapshot = new()
    {
        ActualMmps = 0,
        TargetMmps = 0,
        Status = LineSpeedStatus.Unknown,
        LastUpdatedAt = DateTimeOffset.UtcNow
    };

    private OriginCartSnapshot _originCartSnapshot = new()
    {
        CartId = null,
        LastUpdatedAt = DateTimeOffset.UtcNow
    };

    private ChuteCartSnapshot _chuteCartSnapshot = new()
    {
        Mapping = new Dictionary<long, long?>(),
        LastUpdatedAt = DateTimeOffset.UtcNow
    };

    private ParcelSummary? _lastCreatedParcel;
    private ParcelSummary? _lastDivertedParcel;
    private readonly ConcurrentDictionary<long, ParcelSummary> _onlineParcels = new();

    private DeviceStatusSnapshot _deviceStatusSnapshot = new()
    {
        Status = DeviceStatus.Idle,
        Message = null,
        LastUpdatedAt = DateTimeOffset.UtcNow
    };

    private CartLayoutSnapshot _cartLayoutSnapshot = new()
    {
        CartPositions = Array.Empty<CartPositionSnapshot>(),
        LastUpdatedAt = DateTimeOffset.UtcNow
    };

    private LineRunStateSnapshot _lineRunStateSnapshot = new()
    {
        State = "Idle",
        Message = null,
        LastUpdatedAt = DateTimeOffset.UtcNow
    };

    private SafetyStateSnapshot _safetyStateSnapshot = new()
    {
        State = "Safe",
        Source = null,
        Message = null,
        LastUpdatedAt = DateTimeOffset.UtcNow
    };

    private UpstreamStatusSnapshot _upstreamStatusSnapshot = new()
    {
        Mode = "Disabled",
        ConnectionState = "Disconnected",
        Message = null,
        LastUpdatedAt = DateTimeOffset.UtcNow
    };

    public NarrowBeltLiveView(IEventBus eventBus, ILogger<NarrowBeltLiveView> logger)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
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
            CreatedAt = DateTimeOffset.UtcNow, // 没有创建时间，使用当前时间
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

    public UpstreamStatusSnapshot GetUpstreamStatus()
    {
        lock (_lock)
        {
            return _upstreamStatusSnapshot;
        }
    }

    /// <summary>
    /// 更新上游状态（供外部调用）
    /// </summary>
    public void UpdateUpstreamStatus(string mode, string connectionState, string? message = null)
    {
        lock (_lock)
        {
            _upstreamStatusSnapshot = new UpstreamStatusSnapshot
            {
                Mode = mode,
                ConnectionState = connectionState,
                Message = message,
                LastUpdatedAt = DateTimeOffset.UtcNow
            };
        }
        
        _logger.LogDebug("上游状态已更新: Mode={Mode}, ConnectionState={ConnectionState}", mode, connectionState);
    }

    public void Dispose()
    {
        _logger.LogInformation("实时视图聚合器正在释放资源...");
        
        // 取消订阅事件
        // Note: EventBus 不支持取消订阅，这里只记录日志
        
        _logger.LogInformation("实时视图聚合器已释放");
    }
}

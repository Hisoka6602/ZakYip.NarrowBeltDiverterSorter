using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZakYip.NarrowBeltDiverterSorter.Observability;
using ZakYip.NarrowBeltDiverterSorter.Observability.LiveView;
using ZakYip.NarrowBeltDiverterSorter.Observability.Events;
using ZakYip.NarrowBeltDiverterSorter.Core.Enums;

namespace ZakYip.NarrowBeltDiverterSorter.Host.SignalR;

/// <summary>
/// SignalR 实时推送桥接服务
/// 订阅事件总线并将事件推送到 SignalR 客户端（带推送频率限制）
/// </summary>
public class LiveViewBridgeService : BackgroundService
{
    private readonly IEventBus _eventBus;
    private readonly IHubContext<NarrowBeltLiveHub> _hubContext;
    private readonly INarrowBeltLiveView _liveView;
    private readonly ILogger<LiveViewBridgeService> _logger;
    private readonly LiveViewPushOptions _options;

    // 推送节流器 - 记录最后推送时间
    private DateTime _lastLineSpeedPushTime = DateTime.MinValue;
    private DateTime _lastChuteCartPushTime = DateTime.MinValue;
    private DateTime _lastOriginCartPushTime = DateTime.MinValue;
    private DateTime _lastParcelCreatedPushTime = DateTime.MinValue;
    private DateTime _lastParcelDivertedPushTime = DateTime.MinValue;
    private DateTime _lastDeviceStatusPushTime = DateTime.MinValue;
    private DateTime _lastCartLayoutPushTime = DateTime.MinValue;

    private readonly object _throttleLock = new();
    private Timer? _onlineParcelsTimer;

    public LiveViewBridgeService(
        IEventBus eventBus, 
        IHubContext<NarrowBeltLiveHub> hubContext,
        INarrowBeltLiveView liveView,
        IOptions<LiveViewPushOptions> options,
        ILogger<LiveViewBridgeService> logger)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        _liveView = liveView ?? throw new ArgumentNullException(nameof(liveView));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? new LiveViewPushOptions();
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // 订阅所有实时监控事件
        _eventBus.Subscribe<LineSpeedChangedEventArgs>(OnLineSpeedChangedAsync);
        _eventBus.Subscribe<CartAtChuteChangedEventArgs>(OnCartAtChuteChangedAsync);
        _eventBus.Subscribe<OriginCartChangedEventArgs>(OnOriginCartChangedAsync);
        _eventBus.Subscribe<ParcelCreatedEventArgs>(OnParcelCreatedAsync);
        _eventBus.Subscribe<ParcelDivertedEventArgs>(OnParcelDivertedAsync);
        _eventBus.Subscribe<DeviceStatusChangedEventArgs>(OnDeviceStatusChangedAsync);
        _eventBus.Subscribe<CartLayoutChangedEventArgs>(OnCartLayoutChangedAsync);
        _eventBus.Subscribe<LineRunStateChangedEventArgs>(OnLineRunStateChangedAsync);
        _eventBus.Subscribe<SafetyStateChangedEventArgs>(OnSafetyStateChangedAsync);

        _logger.LogInformation("实时推送桥接服务已启动");
        _logger.LogInformation("推送间隔配置: 速度={0}ms, 格口小车={1}ms, 原点小车={2}ms, 包裹创建={3}ms, 包裹落格={4}ms, 设备状态={5}ms, 小车布局={6}ms",
            _options.LineSpeedPushIntervalMs,
            _options.ChuteCartPushIntervalMs,
            _options.OriginCartPushIntervalMs,
            _options.ParcelCreatedPushIntervalMs,
            _options.ParcelDivertedPushIntervalMs,
            _options.DeviceStatusPushIntervalMs,
            _options.CartLayoutPushIntervalMs);

        // 启动在线包裹列表周期推送定时器
        if (_options.EnableOnlineParcelsPush)
        {
            _onlineParcelsTimer = new Timer(
                PushOnlineParcelsPeriodically,
                null,
                TimeSpan.FromMilliseconds(_options.OnlineParcelsPushPeriodMs),
                TimeSpan.FromMilliseconds(_options.OnlineParcelsPushPeriodMs));
            
            _logger.LogInformation("在线包裹列表周期推送已启用，周期: {Period}ms", _options.OnlineParcelsPushPeriodMs);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 检查是否可以推送（基于时间间隔限制）
    /// </summary>
    private bool CanPush(ref DateTime lastPushTime, int intervalMs)
    {
        lock (_throttleLock)
        {
            var now = DateTime.Now;
            var elapsed = (now - lastPushTime).TotalMilliseconds;
            
            if (elapsed >= intervalMs)
            {
                lastPushTime = now;
                return true;
            }
            
            return false;
        }
    }

    private async Task OnLineSpeedChangedAsync(LineSpeedChangedEventArgs eventArgs, CancellationToken cancellationToken)
    {
        if (!CanPush(ref _lastLineSpeedPushTime, _options.LineSpeedPushIntervalMs))
        {
            _logger.LogTrace("主线速度推送被节流");
            return;
        }

        try
        {
            var dto = new LineSpeedDto
            {
                ActualMmps = eventArgs.ActualMmps,
                TargetMmps = eventArgs.TargetMmps,
                Status = eventArgs.Status.ToString(),
                LastUpdatedAt = eventArgs.OccurredAt
            };

            await _hubContext.Clients.All.SendAsync("LineSpeedUpdated", dto, cancellationToken);
            _logger.LogTrace("已推送主线速度更新");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "推送主线速度更新失败");
        }
    }

    private async Task OnCartAtChuteChangedAsync(CartAtChuteChangedEventArgs eventArgs, CancellationToken cancellationToken)
    {
        if (!CanPush(ref _lastChuteCartPushTime, _options.ChuteCartPushIntervalMs))
        {
            _logger.LogTrace("格口小车推送被节流");
            return;
        }

        try
        {
            var dto = new ChuteCartDto
            {
                ChuteId = eventArgs.ChuteId,
                CartId = eventArgs.CartId
            };

            // 推送给所有客户端
            await _hubContext.Clients.All.SendAsync("ChuteCartChanged", dto, cancellationToken);

            // 推送给订阅该格口的客户端
            var groupName = $"chute:{eventArgs.ChuteId}";
            await _hubContext.Clients.Group(groupName).SendAsync("ChuteCartChanged", dto, cancellationToken);

            _logger.LogTrace("已推送格口小车更新: 格口 {ChuteId}", eventArgs.ChuteId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "推送格口小车更新失败");
        }
    }

    private async Task OnOriginCartChangedAsync(OriginCartChangedEventArgs eventArgs, CancellationToken cancellationToken)
    {
        if (!CanPush(ref _lastOriginCartPushTime, _options.OriginCartPushIntervalMs))
        {
            _logger.LogTrace("原点小车推送被节流");
            return;
        }

        try
        {
            var dto = new OriginCartDto
            {
                CartId = eventArgs.CartId,
                LastUpdatedAt = eventArgs.OccurredAt
            };

            await _hubContext.Clients.All.SendAsync("OriginCartChanged", dto, cancellationToken);
            _logger.LogTrace("已推送原点小车更新");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "推送原点小车更新失败");
        }
    }

    private async Task OnParcelCreatedAsync(ParcelCreatedEventArgs eventArgs, CancellationToken cancellationToken)
    {
        if (!CanPush(ref _lastParcelCreatedPushTime, _options.ParcelCreatedPushIntervalMs))
        {
            _logger.LogTrace("包裹创建推送被节流");
            return;
        }

        try
        {
            var dto = new ParcelDto
            {
                ParcelId = eventArgs.ParcelId,
                Barcode = eventArgs.Barcode,
                WeightKg = eventArgs.WeightKg,
                VolumeCubicMm = eventArgs.VolumeCubicMm,
                TargetChuteId = eventArgs.TargetChuteId,
                CreatedAt = eventArgs.CreatedAt
            };

            await _hubContext.Clients.All.SendAsync("LastCreatedParcelUpdated", dto, cancellationToken);
            _logger.LogTrace("已推送包裹创建更新: {ParcelId}", eventArgs.ParcelId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "推送包裹创建更新失败");
        }
    }

    private async Task OnParcelDivertedAsync(ParcelDivertedEventArgs eventArgs, CancellationToken cancellationToken)
    {
        if (!CanPush(ref _lastParcelDivertedPushTime, _options.ParcelDivertedPushIntervalMs))
        {
            _logger.LogTrace("包裹落格推送被节流");
            return;
        }

        try
        {
            var dto = new ParcelDto
            {
                ParcelId = eventArgs.ParcelId,
                Barcode = eventArgs.Barcode,
                WeightKg = eventArgs.WeightKg,
                VolumeCubicMm = eventArgs.VolumeCubicMm,
                TargetChuteId = eventArgs.TargetChuteId,
                ActualChuteId = eventArgs.ActualChuteId,
                CreatedAt = DateTimeOffset.Now, // 没有创建时间
                DivertedAt = eventArgs.DivertedAt
            };

            await _hubContext.Clients.All.SendAsync("LastDivertedParcelUpdated", dto, cancellationToken);
            _logger.LogTrace("已推送包裹落格更新: {ParcelId}", eventArgs.ParcelId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "推送包裹落格更新失败");
        }
    }

    private async Task OnDeviceStatusChangedAsync(DeviceStatusChangedEventArgs eventArgs, CancellationToken cancellationToken)
    {
        if (!CanPush(ref _lastDeviceStatusPushTime, _options.DeviceStatusPushIntervalMs))
        {
            _logger.LogTrace("设备状态推送被节流");
            return;
        }

        try
        {
            var dto = new DeviceStatusDto
            {
                Status = eventArgs.Status.ToString(),
                Message = eventArgs.Message,
                LastUpdatedAt = eventArgs.OccurredAt
            };

            await _hubContext.Clients.All.SendAsync("DeviceStatusUpdated", dto, cancellationToken);
            _logger.LogTrace("已推送设备状态更新");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "推送设备状态更新失败");
        }
    }

    private async Task OnCartLayoutChangedAsync(CartLayoutChangedEventArgs eventArgs, CancellationToken cancellationToken)
    {
        if (!CanPush(ref _lastCartLayoutPushTime, _options.CartLayoutPushIntervalMs))
        {
            _logger.LogTrace("小车布局推送被节流");
            return;
        }

        try
        {
            var dto = new CartLayoutDto
            {
                CartPositions = eventArgs.CartPositions
                    .Select(cp => new CartPositionDto
                    {
                        CartId = cp.CartId,
                        CartIndex = cp.CartIndex,
                        LinearPositionMm = cp.LinearPositionMm,
                        CurrentChuteId = cp.CurrentChuteId
                    })
                    .ToList(),
                LastUpdatedAt = eventArgs.OccurredAt
            };

            await _hubContext.Clients.All.SendAsync("CartLayoutUpdated", dto, cancellationToken);

            // 同时更新格口小车映射
            var chuteCartDtos = eventArgs.ChuteToCartMapping
                .Select(kvp => new ChuteCartDto { ChuteId = kvp.Key, CartId = kvp.Value })
                .ToList();
            await _hubContext.Clients.All.SendAsync("ChuteCartsUpdated", chuteCartDtos, cancellationToken);

            _logger.LogTrace("已推送小车布局更新");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "推送小车布局更新失败");
        }
    }

    private async Task OnLineRunStateChangedAsync(LineRunStateChangedEventArgs eventArgs, CancellationToken cancellationToken)
    {
        try
        {
            var dto = new LineRunStateDto
            {
                State = eventArgs.State,
                Message = eventArgs.Message,
                LastUpdatedAt = eventArgs.OccurredAt
            };

            await _hubContext.Clients.All.SendAsync("LineRunStateUpdated", dto, cancellationToken);
            _logger.LogInformation("已推送线体运行状态更新: {State}", eventArgs.State);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "推送线体运行状态更新失败");
        }
    }

    private async Task OnSafetyStateChangedAsync(SafetyStateChangedEventArgs eventArgs, CancellationToken cancellationToken)
    {
        try
        {
            var dto = new SafetyStateDto
            {
                State = eventArgs.State,
                Source = eventArgs.Source,
                Message = eventArgs.Message,
                LastUpdatedAt = eventArgs.OccurredAt
            };

            await _hubContext.Clients.All.SendAsync("SafetyStateUpdated", dto, cancellationToken);
            _logger.LogWarning("已推送安全状态更新: {State}, 源: {Source}", eventArgs.State, eventArgs.Source);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "推送安全状态更新失败");
        }
    }

    /// <summary>
    /// 周期性推送在线包裹列表
    /// </summary>
    private async void PushOnlineParcelsPeriodically(object? state)
    {
        try
        {
            var onlineParcels = _liveView.GetOnlineParcels();
            var dtos = onlineParcels.Select(p => new ParcelDto
            {
                ParcelId = p.ParcelId,
                Barcode = p.Barcode,
                WeightKg = p.WeightKg,
                VolumeCubicMm = p.VolumeCubicMm,
                TargetChuteId = p.TargetChuteId,
                ActualChuteId = p.ActualChuteId,
                CreatedAt = p.CreatedAt,
                DivertedAt = p.DivertedAt
            }).ToList();

            await _hubContext.Clients.All.SendAsync("OnlineParcelsUpdated", dtos);
            _logger.LogTrace("已推送在线包裹列表，共 {Count} 个包裹", dtos.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "周期推送在线包裹列表失败");
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("实时推送桥接服务正在停止...");
        
        _onlineParcelsTimer?.Dispose();
        
        return base.StopAsync(cancellationToken);
    }
}

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Observability;
using ZakYip.NarrowBeltDiverterSorter.Observability.LiveView;

namespace ZakYip.NarrowBeltDiverterSorter.Host.SignalR;

/// <summary>
/// SignalR 实时推送桥接服务
/// 订阅事件总线并将事件推送到 SignalR 客户端
/// </summary>
public class LiveViewBridgeService : BackgroundService
{
    private readonly IEventBus _eventBus;
    private readonly IHubContext<NarrowBeltLiveHub> _hubContext;
    private readonly ILogger<LiveViewBridgeService> _logger;

    public LiveViewBridgeService(
        IEventBus eventBus, 
        IHubContext<NarrowBeltLiveHub> hubContext,
        ILogger<LiveViewBridgeService> logger)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

        _logger.LogInformation("实时推送桥接服务已启动");

        return Task.CompletedTask;
    }

    private async Task OnLineSpeedChangedAsync(LineSpeedChangedEventArgs eventArgs, CancellationToken cancellationToken)
    {
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
                CreatedAt = DateTimeOffset.UtcNow, // 没有创建时间
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

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("实时推送桥接服务正在停止...");
        return base.StopAsync(cancellationToken);
    }
}

using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Observability;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Parcels;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Tracking;
using ZakYip.NarrowBeltDiverterSorter.Core.Application;

namespace ZakYip.NarrowBeltDiverterSorter.Host.EventBridges;

/// <summary>
/// 事件总线桥接服务
/// 订阅 C# 事件并转发到 IEventBus，帮助系统过渡到统一的事件总线
/// 这是一个临时桥接层，待所有组件迁移到 IEventBus 后可以移除
/// </summary>
public class EventBusBridgeService : BackgroundService
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<EventBusBridgeService> _logger;
    private readonly IParcelLifecycleTracker? _lifecycleTracker;
    private readonly ICartRingBuilder? _cartRingBuilder;
    private readonly ParcelLoadCoordinator? _loadCoordinator;

    public EventBusBridgeService(
        IEventBus eventBus,
        ILogger<EventBusBridgeService> logger,
        IParcelLifecycleTracker? lifecycleTracker = null,
        ICartRingBuilder? cartRingBuilder = null,
        ParcelLoadCoordinator? loadCoordinator = null)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _lifecycleTracker = lifecycleTracker;
        _cartRingBuilder = cartRingBuilder;
        _loadCoordinator = loadCoordinator;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // 订阅 C# 事件并桥接到 IEventBus
        if (_lifecycleTracker != null)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            _lifecycleTracker.LifecycleChanged += OnLifecycleChanged;
#pragma warning restore CS0618
            _logger.LogInformation("已桥接 ParcelLifecycleTracker 事件到 IEventBus");
        }

        if (_cartRingBuilder != null)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            _cartRingBuilder.OnCartPassed += OnCartPassed;
#pragma warning restore CS0618
            _logger.LogInformation("已桥接 CartRingBuilder 事件到 IEventBus");
        }

        if (_loadCoordinator != null)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            _loadCoordinator.ParcelLoadedOnCart += OnParcelLoadedOnCart;
#pragma warning restore CS0618
            _logger.LogInformation("已桥接 ParcelLoadCoordinator 事件到 IEventBus");
        }

        return Task.CompletedTask;
    }

    private async void OnLifecycleChanged(object? sender, Core.Domain.Parcels.ParcelLifecycleChangedEventArgs e)
    {
        var busEvent = new Observability.Events.ParcelLifecycleChangedEventArgs
        {
            ParcelId = e.ParcelId.Value,
            Status = e.Status.ToString(),
            FailureReason = e.FailureReason != Core.Domain.ParcelFailureReason.None ? e.FailureReason.ToString() : null,
            Remarks = e.Remarks,
            OccurredAt = e.OccurredAt
        };

        await _eventBus.PublishAsync(busEvent);
    }

    private async void OnCartPassed(object? sender, Core.Domain.Tracking.CartPassedEventArgs e)
    {
        var busEvent = new Observability.Events.CartPassedEventArgs
        {
            CartId = e.CartId.Value,
            PassedAt = e.PassAt
        };

        await _eventBus.PublishAsync(busEvent);
    }

    private async void OnParcelLoadedOnCart(object? sender, Core.Domain.Feeding.ParcelLoadedOnCartEventArgs e)
    {
        var busEvent = new Observability.Events.ParcelLoadedOnCartEventArgs
        {
            ParcelId = e.ParcelId.Value,
            CartId = e.CartId.Value,
            LoadedAt = e.LoadedTime
        };

        await _eventBus.PublishAsync(busEvent);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        // 取消订阅
        if (_lifecycleTracker != null)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            _lifecycleTracker.LifecycleChanged -= OnLifecycleChanged;
#pragma warning restore CS0618
        }

        if (_cartRingBuilder != null)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            _cartRingBuilder.OnCartPassed -= OnCartPassed;
#pragma warning restore CS0618
        }

        if (_loadCoordinator != null)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            _loadCoordinator.ParcelLoadedOnCart -= OnParcelLoadedOnCart;
#pragma warning restore CS0618
        }

        return base.StopAsync(cancellationToken);
    }
}

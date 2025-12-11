using ZakYip.NarrowBeltDiverterSorter.Core.Configuration;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Application;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Feeding;
using ZakYip.NarrowBeltDiverterSorter.Core.Enums.Configuration;
using ZakYip.NarrowBeltDiverterSorter.Core.Enums.Domain;

namespace ZakYip.NarrowBeltDiverterSorter.Host;

/// <summary>
/// 包裹装载协调器工作器
/// 包装 ParcelLoadCoordinator 作为后台服务
/// </summary>
public class ParcelLoadCoordinatorWorker : BackgroundService
{
    private readonly ILogger<ParcelLoadCoordinatorWorker> _logger;
    private readonly ParcelLoadCoordinator _coordinator;
    private readonly IEventBus _eventBus;
    private readonly IParcelTimelineService _timelineService;
    private readonly bool _enableBringupLogging;

    public ParcelLoadCoordinatorWorker(
        ILogger<ParcelLoadCoordinatorWorker> logger,
        IParcelLoadPlanner loadPlanner,
        IEventBus eventBus,
        IParcelTimelineService timelineService,
        StartupModeConfiguration startupConfig)
    {
        _logger = logger;
        _coordinator = new ParcelLoadCoordinator(loadPlanner);
        _eventBus = eventBus;
        _timelineService = timelineService;
        _enableBringupLogging = startupConfig.EnableBringupLogging && 
                                startupConfig.Mode >= StartupMode.BringupInfeed;

        // 设置日志输出（Bring-up 模式）
        if (_enableBringupLogging)
        {
            _coordinator.SetLogAction(message => _logger.LogInformation(message));
        }

        // 订阅包裹创建事件（现在直接使用Core类型）
        _eventBus.Subscribe<Core.Domain.Feeding.ParcelCreatedFromInfeedEventArgs>(async (eventArgs, ct) =>
        {
            // 直接使用eventArgs，无需转换
            _coordinator.HandleParcelCreatedFromInfeed(this, eventArgs);
            await Task.CompletedTask;
        });

        // 订阅装载完成事件（Bring-up 模式日志）
        if (_enableBringupLogging)
        {
            _eventBus.Subscribe<Core.Domain.Feeding.ParcelLoadedOnCartEventArgs>(async (eventArgs, ct) =>
            {
                _logger.LogInformation(
                    "入口触发 ParcelId={ParcelId}, 预计落车 CartId={CartId}",
                    eventArgs.ParcelId,
                    eventArgs.CartId);

                // 记录装载到小车时间线事件
                _timelineService.Append(new Core.Domain.Parcels.ParcelTimelineEventArgs
                {
                    ParcelId = eventArgs.ParcelId.Value,
                    EventType = ParcelTimelineEventType.LoadedToCart,
                    OccurredAt = eventArgs.LoadedTime,
                    CartId = eventArgs.CartId.Value,
                    Note = $"包裹装载到小车 {eventArgs.CartId}"
                });

                await Task.CompletedTask;
            });
        }
        else
        {
            // 非 Bring-up 模式也需要记录时间线
            _eventBus.Subscribe<Core.Domain.Feeding.ParcelLoadedOnCartEventArgs>(async (eventArgs, ct) =>
            {
                _timelineService.Append(new Core.Domain.Parcels.ParcelTimelineEventArgs
                {
                    ParcelId = eventArgs.ParcelId.Value,
                    EventType = ParcelTimelineEventType.LoadedToCart,
                    OccurredAt = eventArgs.LoadedTime,
                    CartId = eventArgs.CartId.Value,
                    Note = $"包裹装载到小车 {eventArgs.CartId}"
                });
                await Task.CompletedTask;
            });
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("包裹装载协调器已启动");

        try
        {
            // 保持运行直到取消
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("包裹装载协调器正在停止...");
        }
        finally
        {
            _logger.LogInformation("包裹装载协调器已停止");
        }
    }
}

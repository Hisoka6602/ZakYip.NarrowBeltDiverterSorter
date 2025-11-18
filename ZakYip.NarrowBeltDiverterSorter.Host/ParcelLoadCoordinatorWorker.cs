using ZakYip.NarrowBeltDiverterSorter.Core.Configuration;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Application;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Feeding;
using ZakYip.NarrowBeltDiverterSorter.Observability.Events;

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
    private readonly bool _enableBringupLogging;

    public ParcelLoadCoordinatorWorker(
        ILogger<ParcelLoadCoordinatorWorker> logger,
        IParcelLoadPlanner loadPlanner,
        IEventBus eventBus,
        StartupModeConfiguration startupConfig)
    {
        _logger = logger;
        _coordinator = new ParcelLoadCoordinator(loadPlanner);
        _eventBus = eventBus;
        _enableBringupLogging = startupConfig.EnableBringupLogging && 
                                startupConfig.Mode >= StartupMode.BringupInfeed;

        // 设置日志输出（Bring-up 模式）
        if (_enableBringupLogging)
        {
            _coordinator.SetLogAction(message => _logger.LogInformation(message));
        }

        // 订阅包裹创建事件
        _eventBus.Subscribe<Observability.Events.ParcelCreatedFromInfeedEventArgs>(async (eventArgs, ct) =>
        {
            // 转换为Core事件参数类型
            var coreEventArgs = new Core.Domain.Feeding.ParcelCreatedFromInfeedEventArgs
            {
                ParcelId = new Core.Domain.ParcelId(eventArgs.ParcelId),
                Barcode = eventArgs.Barcode,
                InfeedTriggerTime = eventArgs.InfeedTriggerTime
            };
            _coordinator.HandleParcelCreatedFromInfeed(this, coreEventArgs);
            await Task.CompletedTask;
        });

        // 订阅装载完成事件（Bring-up 模式日志）
        if (_enableBringupLogging)
        {
            _eventBus.Subscribe<Observability.Events.ParcelLoadedOnCartEventArgs>(async (eventArgs, ct) =>
            {
                _logger.LogInformation(
                    "入口触发 ParcelId={ParcelId}, 预计落车 CartId={CartId}",
                    eventArgs.ParcelId,
                    eventArgs.CartId);
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

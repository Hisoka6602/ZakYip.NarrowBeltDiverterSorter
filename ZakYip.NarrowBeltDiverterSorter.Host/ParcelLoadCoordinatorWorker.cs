using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Application;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Feeding;
using ZakYip.NarrowBeltDiverterSorter.Observability;

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

        // 订阅包裹创建事件（需要适配器）
        _eventBus.Subscribe<ParcelCreatedFromInfeedEventArgs>(async (eventArgs, ct) =>
        {
            _coordinator.HandleParcelCreatedFromInfeed(this, eventArgs);
            await Task.CompletedTask;
        });
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("包裹装载协调器已启动");

        // 订阅装载完成事件，输出 Bring-up 日志
        _coordinator.ParcelLoadedOnCart += OnParcelLoadedOnCart;

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
            _coordinator.ParcelLoadedOnCart -= OnParcelLoadedOnCart;
            _logger.LogInformation("包裹装载协调器已停止");
        }
    }

    /// <summary>
    /// 处理包裹装载到小车事件（Bring-up 模式日志）
    /// </summary>
    private void OnParcelLoadedOnCart(object? sender, ParcelLoadedOnCartEventArgs e)
    {
        if (_enableBringupLogging)
        {
            _logger.LogInformation(
                "入口触发 ParcelId={ParcelId}, 预计落车 CartId={CartId}",
                e.ParcelId.Value,
                e.CartId.Value);
        }
    }
}

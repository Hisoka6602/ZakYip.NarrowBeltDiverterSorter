using ZakYip.NarrowBeltDiverterSorter.Ingress.Safety;

namespace ZakYip.NarrowBeltDiverterSorter.Host;

/// <summary>
/// 面板按钮监控工作器
/// 作为 ASP.NET BackgroundService 的薄壳，将监控逻辑委托给 PanelButtonMonitor
/// </summary>
public class PanelButtonMonitorWorker : BackgroundService
{
    private readonly PanelButtonMonitor _monitor;
    private readonly ILogger<PanelButtonMonitorWorker> _logger;

    public PanelButtonMonitorWorker(
        PanelButtonMonitor monitor,
        ILogger<PanelButtonMonitorWorker> logger)
    {
        _monitor = monitor;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("面板按钮监控工作器已启动");
        return _monitor.RunAsync(stoppingToken);
    }
}

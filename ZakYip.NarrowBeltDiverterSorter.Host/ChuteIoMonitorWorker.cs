using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using Microsoft.Extensions.Options;
using ZakYip.NarrowBeltDiverterSorter.Communication;
using ZakYip.NarrowBeltDiverterSorter.Ingress.Chute;
using ZakYip.NarrowBeltDiverterSorter.Observability;

namespace ZakYip.NarrowBeltDiverterSorter.Host;

/// <summary>
/// 格口IO监视器工作器
/// 包装 ChuteIoMonitor 作为后台服务
/// </summary>
public class ChuteIoMonitorWorker : BackgroundService
{
    private readonly ILogger<ChuteIoMonitorWorker> _logger;
    private readonly ChuteIoMonitor _monitor;

    public ChuteIoMonitorWorker(
        ILogger<ChuteIoMonitorWorker> logger,
        IFieldBusClient fieldBusClient,
        IOptions<ChuteIoMonitorConfiguration> configuration,
        IEventBus eventBus,
        ILogger<ChuteIoMonitor> monitorLogger)
    {
        _logger = logger;
        _monitor = new ChuteIoMonitor(fieldBusClient, configuration.Value, eventBus, monitorLogger);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("格口IO监视器已启动");

        _ = _monitor.StartAsync(stoppingToken);

        // Wait for cancellation
        return Task.Run(async () =>
        {
            try
            {
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("格口IO监视器正在停止...");
                await _monitor.StopAsync();
                _logger.LogInformation("格口IO监视器已停止");
            }
        }, stoppingToken);
    }
}

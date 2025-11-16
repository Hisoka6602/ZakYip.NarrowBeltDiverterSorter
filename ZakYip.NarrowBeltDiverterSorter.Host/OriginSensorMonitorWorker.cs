using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Tracking;
using ZakYip.NarrowBeltDiverterSorter.Ingress.Origin;

namespace ZakYip.NarrowBeltDiverterSorter.Host;

/// <summary>
/// 原点传感器监视器工作器
/// 包装 OriginSensorMonitor 作为后台服务
/// </summary>
public class OriginSensorMonitorWorker : BackgroundService
{
    private readonly ILogger<OriginSensorMonitorWorker> _logger;
    private readonly OriginSensorMonitor _monitor;

    public OriginSensorMonitorWorker(
        ILogger<OriginSensorMonitorWorker> logger,
        IOriginSensorPort originSensorPort,
        ICartRingBuilder cartRingBuilder)
    {
        _logger = logger;
        _monitor = new OriginSensorMonitor(originSensorPort, cartRingBuilder);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("原点传感器监视器已启动");

        _monitor.Start();

        // Wait for cancellation
        return Task.Run(async () =>
        {
            try
            {
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("原点传感器监视器正在停止...");
                await _monitor.StopAsync();
                _logger.LogInformation("原点传感器监视器已停止");
            }
        }, stoppingToken);
    }
}

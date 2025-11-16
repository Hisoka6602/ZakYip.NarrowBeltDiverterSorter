using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Ingress.Infeed;

namespace ZakYip.NarrowBeltDiverterSorter.Host;

/// <summary>
/// 入口传感器监视器工作器
/// 包装 InfeedSensorMonitor 作为后台服务
/// </summary>
public class InfeedSensorMonitorWorker : BackgroundService
{
    private readonly ILogger<InfeedSensorMonitorWorker> _logger;
    private readonly InfeedSensorMonitor _monitor;

    public InfeedSensorMonitorWorker(
        ILogger<InfeedSensorMonitorWorker> logger,
        IInfeedSensorPort infeedSensorPort)
    {
        _logger = logger;
        _monitor = new InfeedSensorMonitor(infeedSensorPort);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("入口传感器监视器已启动");

        try
        {
            await _monitor.StartAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("入口传感器监视器正在停止...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "入口传感器监视器发生异常");
        }
        finally
        {
            await _monitor.StopAsync();
            _logger.LogInformation("入口传感器监视器已停止");
        }
    }
}

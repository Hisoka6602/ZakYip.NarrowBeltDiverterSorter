using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Ingress.Infeed;

namespace ZakYip.NarrowBeltDiverterSorter.Simulation;

/// <summary>
/// 入口传感器监视器托管服务
/// 包装 InfeedSensorMonitor 作为后台服务
/// </summary>
public class InfeedSensorMonitorHostedService : BackgroundService
{
    private readonly ILogger<InfeedSensorMonitorHostedService> _logger;
    private readonly InfeedSensorMonitor _monitor;

    public InfeedSensorMonitorHostedService(
        ILogger<InfeedSensorMonitorHostedService> logger,
        InfeedSensorMonitor monitor)
    {
        _logger = logger;
        _monitor = monitor;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("入口传感器监视器已启动");

        try
        {
            await _monitor.StartAsync(stoppingToken);
            
            // 保持运行直到取消
            await Task.Delay(Timeout.Infinite, stoppingToken);
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

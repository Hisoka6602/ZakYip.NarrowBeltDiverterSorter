using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Ingress.Origin;

namespace ZakYip.NarrowBeltDiverterSorter.Simulation;

/// <summary>
/// 原点传感器监视器托管服务
/// 包装 OriginSensorMonitor 作为后台服务
/// </summary>
public class OriginSensorMonitorHostedService : BackgroundService
{
    private readonly ILogger<OriginSensorMonitorHostedService> _logger;
    private readonly OriginSensorMonitor _monitor;

    public OriginSensorMonitorHostedService(
        ILogger<OriginSensorMonitorHostedService> logger,
        OriginSensorMonitor monitor)
    {
        _logger = logger;
        _monitor = monitor;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("原点传感器监视器已启动");

        try
        {
            _monitor.Start();
            
            // 保持运行直到取消
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("原点传感器监视器正在停止...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "原点传感器监视器发生异常");
        }
        finally
        {
            await _monitor.StopAsync();
            _logger.LogInformation("原点传感器监视器已停止");
        }
    }
}

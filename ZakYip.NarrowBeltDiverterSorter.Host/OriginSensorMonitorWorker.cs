using ZakYip.NarrowBeltDiverterSorter.Core.Configuration;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Tracking;
using ZakYip.NarrowBeltDiverterSorter.Ingress.Origin;
using ZakYip.NarrowBeltDiverterSorter.Observability;

namespace ZakYip.NarrowBeltDiverterSorter.Host;

/// <summary>
/// 原点传感器监视器工作器
/// 包装 OriginSensorMonitor 作为后台服务
/// </summary>
public class OriginSensorMonitorWorker : BackgroundService
{
    private readonly ILogger<OriginSensorMonitorWorker> _logger;
    private readonly OriginSensorMonitor _monitor;
    private readonly ICartRingBuilder _cartRingBuilder;
    private readonly ICartPositionTracker _cartPositionTracker;
    private readonly bool _enableBringupLogging;

    public OriginSensorMonitorWorker(
        ILogger<OriginSensorMonitorWorker> logger,
        IOriginSensorPort originSensorPort,
        ICartRingBuilder cartRingBuilder,
        ICartPositionTracker cartPositionTracker,
        IEventBus eventBus,
        ILogger<OriginSensorMonitor> monitorLogger,
        StartupModeConfiguration startupConfig)
    {
        _logger = logger;
        _monitor = new OriginSensorMonitor(originSensorPort, cartRingBuilder, cartPositionTracker, eventBus, monitorLogger);
        _cartRingBuilder = cartRingBuilder;
        _cartPositionTracker = cartPositionTracker;
        _enableBringupLogging = startupConfig.EnableBringupLogging && 
                                startupConfig.Mode >= StartupMode.BringupMainline;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("原点传感器监视器已启动");

        _ = _monitor.StartAsync(stoppingToken);

        // Wait for cancellation
        return Task.Run(async () =>
        {
            try
            {
                // 如果启用 Bring-up 日志，周期性输出小车环状态
                if (_enableBringupLogging)
                {
                    await LogCartRingStatusPeriodically(stoppingToken);
                }
                else
                {
                    await Task.Delay(Timeout.Infinite, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("原点传感器监视器正在停止...");
                await _monitor.StopAsync();
                _logger.LogInformation("原点传感器监视器已停止");
            }
        }, stoppingToken);
    }

    /// <summary>
    /// 周期性输出小车环状态（Bring-up 模式）
    /// </summary>
    private async Task LogCartRingStatusPeriodically(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var snapshot = _cartRingBuilder.CurrentSnapshot;
                var isBuilt = snapshot != null;
                var ringLength = snapshot?.RingLength.Value ?? 0;
                var zeroCartId = snapshot?.ZeroCartId.Value ?? 0;

                _logger.LogInformation(
                    "[原点状态] 小车环已构建: {IsBuilt}, 环长度: {RingLength}, ZeroCartId: {ZeroCartId}",
                    isBuilt ? "是" : "否",
                    ringLength,
                    zeroCartId);

                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "输出小车环状态时发生异常");
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
    }
}

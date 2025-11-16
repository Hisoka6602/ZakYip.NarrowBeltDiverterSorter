using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Tracking;
using ZakYip.NarrowBeltDiverterSorter.Simulation.Fakes;

namespace ZakYip.NarrowBeltDiverterSorter.Simulation;

/// <summary>
/// 包裹生成器后台服务
/// 定时产生包裹并触发入口传感器
/// </summary>
public class ParcelGeneratorWorker : BackgroundService
{
    private readonly SimulationConfiguration _config;
    private readonly FakeInfeedSensorPort _infeedSensor;
    private readonly ICartPositionTracker _cartPositionTracker;
    private readonly ILogger<ParcelGeneratorWorker> _logger;
    private long _parcelIdCounter = 1;

    public ParcelGeneratorWorker(
        SimulationConfiguration config,
        FakeInfeedSensorPort infeedSensor,
        ICartPositionTracker cartPositionTracker,
        ILogger<ParcelGeneratorWorker> logger)
    {
        _config = config;
        _infeedSensor = infeedSensor;
        _cartPositionTracker = cartPositionTracker;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("包裹生成器已启动，等待小车环就绪...");

        // Wait for cart ring to be ready
        const int maxWaitSeconds = 30;
        var timeout = DateTimeOffset.UtcNow.AddSeconds(maxWaitSeconds);
        
        while (!stoppingToken.IsCancellationRequested && DateTimeOffset.UtcNow < timeout)
        {
            if (_cartPositionTracker.IsInitialized)
            {
                _logger.LogInformation("小车环已就绪，开始生成包裹");
                break;
            }
            
            await Task.Delay(500, stoppingToken);
        }
        
        if (!_cartPositionTracker.IsInitialized)
        {
            _logger.LogWarning("等待小车环就绪超时，包裹生成器可能无法正常工作");
        }

        // Additional delay for system stabilization
        await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);

        var intervalMs = (int)(_config.ParcelGenerationIntervalSeconds * 1000);
        
        // 确定停止条件
        var maxParcels = _config.ParcelCount > 0 ? _config.ParcelCount : int.MaxValue;
        var stopTime = _config.SimulationDurationSeconds > 0
            ? DateTimeOffset.Now.AddSeconds(_config.SimulationDurationSeconds)
            : DateTimeOffset.MaxValue;

        while (!stoppingToken.IsCancellationRequested 
               && _parcelIdCounter <= maxParcels 
               && DateTimeOffset.Now < stopTime)
        {
            try
            {
                // 生成包裹ID（使用毫秒时间戳）
                var parcelId = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                
                _logger.LogDebug("生成包裹 #{Counter} (ID: {ParcelId})", _parcelIdCounter, parcelId);

                // 触发入口传感器
                _infeedSensor.SimulateParcelDetection();

                _parcelIdCounter++;

                await Task.Delay(intervalMs, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "包裹生成过程中发生错误");
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }

        _logger.LogInformation("包裹生成器已停止，共生成 {Count} 个包裹", _parcelIdCounter - 1);
    }
}

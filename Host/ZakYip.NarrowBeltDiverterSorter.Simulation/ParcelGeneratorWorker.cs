using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.MainLine;
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
    private readonly IMainLineSpeedProvider _speedProvider;
    private readonly ILogger<ParcelGeneratorWorker> _logger;
    private long _parcelIdCounter = 1;

    public ParcelGeneratorWorker(
        SimulationConfiguration config,
        FakeInfeedSensorPort infeedSensor,
        ICartPositionTracker cartPositionTracker,
        IMainLineSpeedProvider speedProvider,
        ILogger<ParcelGeneratorWorker> logger)
    {
        _config = config;
        _infeedSensor = infeedSensor;
        _cartPositionTracker = cartPositionTracker;
        _speedProvider = speedProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("包裹生成器已启动，等待系统就绪...");

        // Wait for both cart ring and main line speed to be stable
        const int maxWaitSeconds = 60;
        var timeout = DateTimeOffset.UtcNow.AddSeconds(maxWaitSeconds);
        
        bool cartRingReady = false;
        bool speedStable = false;
        
        while (!stoppingToken.IsCancellationRequested && DateTimeOffset.UtcNow < timeout)
        {
            // Check cart ring readiness
            if (!cartRingReady && _cartPositionTracker.IsRingReady)
            {
                cartRingReady = true;
                _logger.LogInformation("小车环已就绪");
            }
            
            // Check speed stability
            if (!speedStable && _speedProvider.IsSpeedStable)
            {
                speedStable = true;
                _logger.LogInformation(
                    "主线速度已稳定 - 当前速度: {CurrentSpeed:F1} mm/s, 稳定持续: {StableDuration:F1}秒",
                    _speedProvider.CurrentMmps,
                    _speedProvider.StableDuration.TotalSeconds);
            }
            
            // Both conditions met
            if (cartRingReady && speedStable)
            {
                _logger.LogInformation("系统已就绪，开始生成包裹");
                break;
            }
            
            await Task.Delay(500, stoppingToken);
        }
        
        if (!cartRingReady)
        {
            _logger.LogWarning("等待小车环就绪超时，包裹生成器可能无法正常工作");
        }
        
        if (!speedStable)
        {
            _logger.LogWarning(
                "等待主线速度稳定超时 (当前速度: {CurrentSpeed:F1} mm/s)，开始生成包裹但可能影响分拣质量",
                _speedProvider.CurrentMmps);
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

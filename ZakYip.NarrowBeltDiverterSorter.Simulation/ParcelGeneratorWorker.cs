using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<ParcelGeneratorWorker> _logger;
    private long _parcelIdCounter = 1;

    public ParcelGeneratorWorker(
        SimulationConfiguration config,
        FakeInfeedSensorPort infeedSensor,
        ILogger<ParcelGeneratorWorker> logger)
    {
        _config = config;
        _infeedSensor = infeedSensor;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("包裹生成器已启动");

        // 等待系统初始化
        await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);

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
                
                Console.WriteLine($"\n════════════════════════════════════════");
                Console.WriteLine($"[包裹生成器] 生成包裹 #{_parcelIdCounter} (ID: {parcelId})");
                Console.WriteLine($"════════════════════════════════════════");

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

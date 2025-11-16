using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Simulation.Fakes;

namespace ZakYip.NarrowBeltDiverterSorter.Simulation;

/// <summary>
/// 小车运动模拟器
/// 模拟小车在主线上的运动，触发原点传感器事件
/// </summary>
public class CartMovementSimulator : BackgroundService
{
    private readonly SimulationConfiguration _config;
    private readonly FakeOriginSensorPort _originSensor;
    private readonly FakeMainLineFeedbackPort _mainLineFeedback;
    private readonly ILogger<CartMovementSimulator> _logger;
    private int _currentCartIndex = 0;

    public CartMovementSimulator(
        SimulationConfiguration config,
        FakeOriginSensorPort originSensor,
        FakeMainLineFeedbackPort mainLineFeedback,
        ILogger<CartMovementSimulator> logger)
    {
        _config = config;
        _originSensor = originSensor;
        _mainLineFeedback = mainLineFeedback;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("小车运动模拟器已启动");

        // 等待主线启动
        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var currentSpeed = _mainLineFeedback.GetCurrentSpeed();
                
                if (currentSpeed > 0)
                {
                    // 计算小车通过原点的时间间隔
                    // 时间 = 距离 / 速度
                    var cartPassingIntervalMs = (double)(_config.CartSpacingMm / (decimal)currentSpeed * 1000);

                    // 模拟小车通过原点
                    bool isCartZero = (_currentCartIndex == 0);
                    await _originSensor.SimulateCartPassingAsync(isCartZero);

                    if (isCartZero)
                    {
                        _logger.LogDebug("0号车通过原点 - 当前速度: {Speed:F2} mm/s", currentSpeed);
                    }

                    _currentCartIndex = (_currentCartIndex + 1) % _config.NumberOfCarts;

                    await Task.Delay((int)cartPassingIntervalMs, stoppingToken);
                }
                else
                {
                    // 主线停止时，等待
                    await Task.Delay(100, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "小车运动模拟过程中发生错误");
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }

        _logger.LogInformation("小车运动模拟器已停止");
    }
}

using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Tracking;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Ingress;
using ZakYip.NarrowBeltDiverterSorter.Observability;
using Microsoft.Extensions.Logging;

namespace ZakYip.NarrowBeltDiverterSorter.Ingress.Origin;

/// <summary>
/// 原点传感器监视器
/// 周期性轮询两个原点IO状态，检测上升/下降沿，通过IEventBus发布事件
/// </summary>
public class OriginSensorMonitor : IIoMonitor
{
    private readonly IOriginSensorPort _sensorPort;
    private readonly ICartRingBuilder _cartRingBuilder;
    private readonly ICartPositionTracker _cartPositionTracker;
    private readonly IEventBus _eventBus;
    private readonly ILogger<OriginSensorMonitor> _logger;
    private readonly TimeSpan _pollingInterval;
    
    private bool _previousSensor1State = false;
    private bool _previousSensor2State = false;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _monitoringTask;

    /// <summary>
    /// 创建原点传感器监视器
    /// </summary>
    /// <param name="sensorPort">传感器端口</param>
    /// <param name="cartRingBuilder">小车环构建器</param>
    /// <param name="cartPositionTracker">小车位置跟踪器</param>
    /// <param name="eventBus">事件总线</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="pollingInterval">轮询间隔（默认10ms）</param>
    public OriginSensorMonitor(
        IOriginSensorPort sensorPort,
        ICartRingBuilder cartRingBuilder,
        ICartPositionTracker cartPositionTracker,
        IEventBus eventBus,
        ILogger<OriginSensorMonitor> logger,
        TimeSpan? pollingInterval = null)
    {
        _sensorPort = sensorPort ?? throw new ArgumentNullException(nameof(sensorPort));
        _cartRingBuilder = cartRingBuilder ?? throw new ArgumentNullException(nameof(cartRingBuilder));
        _cartPositionTracker = cartPositionTracker ?? throw new ArgumentNullException(nameof(cartPositionTracker));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _pollingInterval = pollingInterval ?? TimeSpan.FromMilliseconds(10);
    }

    /// <inheritdoc/>
    public bool IsRunning => _monitoringTask != null && !_monitoringTask.IsCompleted;

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_monitoringTask != null)
        {
            _logger.LogWarning("原点传感器监视器已在运行中");
            return Task.CompletedTask;
        }

        _logger.LogInformation("启动原点传感器监视器");
        _cancellationTokenSource = new CancellationTokenSource();
        _monitoringTask = Task.Run(async () => await MonitoringLoopAsync(_cancellationTokenSource.Token), cancellationToken);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task StopAsync()
    {
        if (_cancellationTokenSource == null || _monitoringTask == null)
        {
            return;
        }

        _logger.LogInformation("停止原点传感器监视器");
        _cancellationTokenSource.Cancel();
        
        try
        {
            await _monitoringTask;
        }
        catch (OperationCanceledException)
        {
            // Expected when cancelling
        }
        finally
        {
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            _monitoringTask = null;
        }
    }

    private async Task MonitoringLoopAsync(CancellationToken cancellationToken)
    {
        bool bothSensorsWereBlocked = false;
        
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Read current sensor states
                bool sensor1State = _sensorPort.GetFirstSensorState();
                bool sensor2State = _sensorPort.GetSecondSensorState();

                var timestamp = DateTimeOffset.Now;

                // Check for edges on sensor 1
                if (sensor1State != _previousSensor1State)
                {
                    _cartRingBuilder.OnOriginSensorTriggered(
                        isFirstSensor: true,
                        isRisingEdge: sensor1State,
                        timestamp: timestamp);
                    
                    // 发布传感器触发事件
                    var sensorEvent = new SensorTriggeredEventArgs
                    {
                        SensorId = "Origin1",
                        TriggerTime = timestamp,
                        IsTriggered = sensor1State,
                        IsRisingEdge = sensor1State
                    };
                    _ = _eventBus.PublishAsync(sensorEvent);
                    
                    _previousSensor1State = sensor1State;
                }

                // Check for edges on sensor 2
                if (sensor2State != _previousSensor2State)
                {
                    _cartRingBuilder.OnOriginSensorTriggered(
                        isFirstSensor: false,
                        isRisingEdge: sensor2State,
                        timestamp: timestamp);
                    
                    // 发布传感器触发事件
                    var sensorEvent = new SensorTriggeredEventArgs
                    {
                        SensorId = "Origin2",
                        TriggerTime = timestamp,
                        IsTriggered = sensor2State,
                        IsRisingEdge = sensor2State
                    };
                    _ = _eventBus.PublishAsync(sensorEvent);
                    
                    _previousSensor2State = sensor2State;
                }

                // Track when both sensors are blocked (cart is passing)
                if (sensor1State && sensor2State)
                {
                    bothSensorsWereBlocked = true;
                }

                // Detect cart passage completion - when both sensors are unblocked after being blocked
                if (!sensor1State && !sensor2State && bothSensorsWereBlocked)
                {
                    // A cart has completely passed the origin
                    _cartPositionTracker.OnCartPassedOrigin(timestamp);
                    bothSensorsWereBlocked = false;
                }

                await Task.Delay(_pollingInterval, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}

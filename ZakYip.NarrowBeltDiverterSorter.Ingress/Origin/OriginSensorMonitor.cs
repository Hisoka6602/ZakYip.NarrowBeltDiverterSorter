using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Tracking;

namespace ZakYip.NarrowBeltDiverterSorter.Ingress.Origin;

/// <summary>
/// 原点传感器监视器
/// 周期性轮询两个原点IO状态，检测上升/下降沿
/// </summary>
public class OriginSensorMonitor
{
    private readonly IOriginSensorPort _sensorPort;
    private readonly ICartRingBuilder _cartRingBuilder;
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
    /// <param name="pollingInterval">轮询间隔（默认10ms）</param>
    public OriginSensorMonitor(
        IOriginSensorPort sensorPort,
        ICartRingBuilder cartRingBuilder,
        TimeSpan? pollingInterval = null)
    {
        _sensorPort = sensorPort ?? throw new ArgumentNullException(nameof(sensorPort));
        _cartRingBuilder = cartRingBuilder ?? throw new ArgumentNullException(nameof(cartRingBuilder));
        _pollingInterval = pollingInterval ?? TimeSpan.FromMilliseconds(10);
    }

    /// <summary>
    /// 启动监控
    /// </summary>
    public void Start()
    {
        if (_monitoringTask != null)
        {
            return;
        }

        _cancellationTokenSource = new CancellationTokenSource();
        _monitoringTask = Task.Run(async () => await MonitoringLoopAsync(_cancellationTokenSource.Token));
    }

    /// <summary>
    /// 停止监控
    /// </summary>
    public async Task StopAsync()
    {
        if (_cancellationTokenSource == null || _monitoringTask == null)
        {
            return;
        }

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
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Read current sensor states
                bool sensor1State = _sensorPort.GetFirstSensorState();
                bool sensor2State = _sensorPort.GetSecondSensorState();

                var timestamp = DateTimeOffset.UtcNow;

                // Check for edges on sensor 1
                if (sensor1State != _previousSensor1State)
                {
                    _cartRingBuilder.OnOriginSensorTriggered(
                        isFirstSensor: true,
                        isRisingEdge: sensor1State,
                        timestamp: timestamp);
                    _previousSensor1State = sensor1State;
                }

                // Check for edges on sensor 2
                if (sensor2State != _previousSensor2State)
                {
                    _cartRingBuilder.OnOriginSensorTriggered(
                        isFirstSensor: false,
                        isRisingEdge: sensor2State,
                        timestamp: timestamp);
                    _previousSensor2State = sensor2State;
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

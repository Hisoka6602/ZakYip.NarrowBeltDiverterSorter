using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Enums.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Enums.Communication;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Feeding;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Ingress;
using ZakYip.NarrowBeltDiverterSorter.Observability;
using Microsoft.Extensions.Logging;

namespace ZakYip.NarrowBeltDiverterSorter.Ingress.Infeed;

/// <summary>
/// 入口传感器监视器
/// 监听入口IO的上升沿/有效进入信号，通过IEventBus发布事件
/// </summary>
public class InfeedSensorMonitor : IIoMonitor
{
    private readonly IInfeedSensorPort _sensorPort;
    private readonly IEventBus _eventBus;
    private readonly ILogger<InfeedSensorMonitor> _logger;
    private readonly IFeedingBackpressureController? _backpressureController;
    private long _nextParcelIdCounter = 1;
    private bool _isRunning;

    /// <summary>
    /// 创建入口传感器监视器
    /// </summary>
    /// <param name="sensorPort">传感器端口</param>
    /// <param name="eventBus">事件总线</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="backpressureController">供包背压控制器（可选）</param>
    public InfeedSensorMonitor(
        IInfeedSensorPort sensorPort,
        IEventBus eventBus,
        ILogger<InfeedSensorMonitor> logger,
        IFeedingBackpressureController? backpressureController = null)
    {
        _sensorPort = sensorPort ?? throw new ArgumentNullException(nameof(sensorPort));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _backpressureController = backpressureController;
        _sensorPort.ParcelDetected += OnParcelDetected;
    }

    /// <inheritdoc/>
    public bool IsRunning => _isRunning;

    /// <inheritdoc/>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("启动入口传感器监视器");
        await _sensorPort.StartMonitoringAsync(cancellationToken);
        _isRunning = true;
    }

    /// <inheritdoc/>
    public async Task StopAsync()
    {
        _logger.LogInformation("停止入口传感器监视器");
        await _sensorPort.StopMonitoringAsync();
        _isRunning = false;
    }

    private void OnParcelDetected(object? sender, ParcelDetectedEventArgs e)
    {
        // 只处理上升沿（遮挡信号）
        if (!e.IsBlocked)
        {
            return;
        }

        // 检查背压控制
        if (_backpressureController != null)
        {
            var decision = _backpressureController.CheckFeedingAllowed();
            
            if (decision.Decision == FeedingDecision.Reject)
            {
                _backpressureController.RecordPauseEvent();
                _logger.LogWarning(
                    "当前在途包裹数已达上限，启动背压策略：暂停供包。原因：{Reason}，在途数：{InFlight}，上游等待数：{Pending}",
                    decision.Reason,
                    decision.CurrentInFlightCount,
                    decision.CurrentUpstreamPendingCount);
                return;
            }
            
            if (decision.Decision == FeedingDecision.Delay)
            {
                _backpressureController.RecordThrottleEvent();
                _logger.LogWarning(
                    "当前在途包裹数接近上限，启动背压策略：降速供包。原因：{Reason}，在途数：{InFlight}，上游等待数：{Pending}",
                    decision.Reason,
                    decision.CurrentInFlightCount,
                    decision.CurrentUpstreamPendingCount);
                // 注意：实际的降速逻辑需要在调用方实现（例如延长定时器间隔）
                // 这里我们仍然允许创建包裹，但记录了降速事件
            }
        }

        // 生成新的包裹ID
        var parcelId = new ParcelId(_nextParcelIdCounter++);

        // 生成条码（这里使用简单的格式，实际应用中可能需要从其他源获取）
        var barcode = $"PARCEL{parcelId.Value:D10}";

        var eventArgs = new ParcelCreatedFromInfeedEventArgs
        {
            ParcelId = parcelId,
            Barcode = barcode,
            InfeedTriggerTime = e.DetectionTime
        };

        // 发布到事件总线（主要事件）
        _ = _eventBus.PublishAsync(eventArgs);

        // 发布传感器触发事件
        var sensorEvent = new Observability.Events.SensorTriggeredEventArgs
        {
            SensorId = "Infeed",
            TriggerTime = e.DetectionTime,
            IsTriggered = e.IsBlocked,
            IsRisingEdge = true
        };
        _ = _eventBus.PublishAsync(sensorEvent);

        _logger.LogDebug("入口传感器触发，包裹ID: {ParcelId}, 条码: {Barcode}", parcelId, barcode);
    }
}

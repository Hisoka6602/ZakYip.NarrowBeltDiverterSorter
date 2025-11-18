using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
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
    private long _nextParcelIdCounter = 1;
    private bool _isRunning;

    /// <summary>
    /// 包裹从入口创建事件（已废弃，请订阅 IEventBus）
    /// </summary>
    [Obsolete("请使用 IEventBus 订阅 Observability.Events.ParcelCreatedFromInfeedEventArgs，此事件将在未来版本中移除")]
    public event EventHandler<ParcelCreatedFromInfeedEventArgs>? ParcelCreatedFromInfeed;

    /// <summary>
    /// 创建入口传感器监视器
    /// </summary>
    /// <param name="sensorPort">传感器端口</param>
    /// <param name="eventBus">事件总线</param>
    /// <param name="logger">日志记录器</param>
    public InfeedSensorMonitor(
        IInfeedSensorPort sensorPort,
        IEventBus eventBus,
        ILogger<InfeedSensorMonitor> logger)
    {
        _sensorPort = sensorPort ?? throw new ArgumentNullException(nameof(sensorPort));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

        // 生成新的包裹ID
        var parcelId = new ParcelId(_nextParcelIdCounter++);

        // 生成条码（这里使用简单的格式，实际应用中可能需要从其他源获取）
        var barcode = $"PARCEL{parcelId.Value:D10}";

        var coreEventArgs = new ParcelCreatedFromInfeedEventArgs
        {
            ParcelId = parcelId,
            Barcode = barcode,
            InfeedTriggerTime = e.DetectionTime
        };

        // 发布到事件总线（主要事件）
        var busEventArgs = new Observability.Events.ParcelCreatedFromInfeedEventArgs
        {
            ParcelId = parcelId.Value,
            Barcode = barcode,
            InfeedTriggerTime = e.DetectionTime
        };
        _ = _eventBus.PublishAsync(busEventArgs);

        // 同时触发传统事件（向后兼容，已废弃）
#pragma warning disable CS0618 // Type or member is obsolete
        ParcelCreatedFromInfeed?.Invoke(this, coreEventArgs);
#pragma warning restore CS0618

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

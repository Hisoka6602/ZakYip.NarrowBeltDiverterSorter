using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Observability.Events;

namespace ZakYip.NarrowBeltDiverterSorter.Observability.Recording;

/// <summary>
/// 录制事件订阅器
/// 订阅关键领域事件并录制到当前会话
/// </summary>
public class RecordingEventSubscriber : IDisposable
{
    private readonly IEventBus _eventBus;
    private readonly IEventRecorder _recorder;
    private readonly ILogger<RecordingEventSubscriber> _logger;

    public RecordingEventSubscriber(
        IEventBus eventBus,
        IEventRecorder recorder,
        ILogger<RecordingEventSubscriber> logger)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _recorder = recorder ?? throw new ArgumentNullException(nameof(recorder));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        SubscribeToEvents();
    }

    private void SubscribeToEvents()
    {
        // 订阅速度相关事件
        _eventBus.Subscribe<LineSpeedChangedEventArgs>(OnLineSpeedChangedAsync);

        // 订阅包裹相关事件
        _eventBus.Subscribe<ParcelCreatedEventArgs>(OnParcelCreatedAsync);
        _eventBus.Subscribe<ParcelDivertedEventArgs>(OnParcelDivertedAsync);

        // 订阅小车/格口相关事件
        _eventBus.Subscribe<OriginCartChangedEventArgs>(OnOriginCartChangedAsync);
        _eventBus.Subscribe<CartAtChuteChangedEventArgs>(OnCartAtChuteChangedAsync);
        _eventBus.Subscribe<CartLayoutChangedEventArgs>(OnCartLayoutChangedAsync);

        // 订阅安全/运行状态事件
        _eventBus.Subscribe<LineRunStateChangedEventArgs>(OnLineRunStateChangedAsync);
        _eventBus.Subscribe<SafetyStateChangedEventArgs>(OnSafetyStateChangedAsync);

        // 订阅设备状态事件
        _eventBus.Subscribe<DeviceStatusChangedEventArgs>(OnDeviceStatusChangedAsync);

        _logger.LogInformation("Recording event subscriber initialized and subscribed to domain events");
    }

    private async Task OnLineSpeedChangedAsync(LineSpeedChangedEventArgs e, CancellationToken ct)
    {
        await _recorder.RecordAsync("LineSpeedChanged", e, e.OccurredAt, ct: ct);
    }

    private async Task OnParcelCreatedAsync(ParcelCreatedEventArgs e, CancellationToken ct)
    {
        await _recorder.RecordAsync("ParcelCreated", e, e.CreatedAt, ct: ct);
    }

    private async Task OnParcelDivertedAsync(ParcelDivertedEventArgs e, CancellationToken ct)
    {
        await _recorder.RecordAsync("ParcelDiverted", e, e.DivertedAt, ct: ct);
    }

    private async Task OnOriginCartChangedAsync(OriginCartChangedEventArgs e, CancellationToken ct)
    {
        await _recorder.RecordAsync("OriginCartChanged", e, e.OccurredAt, ct: ct);
    }

    private async Task OnCartAtChuteChangedAsync(CartAtChuteChangedEventArgs e, CancellationToken ct)
    {
        await _recorder.RecordAsync("CartAtChuteChanged", e, e.OccurredAt, ct: ct);
    }

    private async Task OnCartLayoutChangedAsync(CartLayoutChangedEventArgs e, CancellationToken ct)
    {
        await _recorder.RecordAsync("CartLayoutChanged", e, e.OccurredAt, ct: ct);
    }

    private async Task OnLineRunStateChangedAsync(LineRunStateChangedEventArgs e, CancellationToken ct)
    {
        await _recorder.RecordAsync("LineRunStateChanged", e, e.OccurredAt, ct: ct);
    }

    private async Task OnSafetyStateChangedAsync(SafetyStateChangedEventArgs e, CancellationToken ct)
    {
        await _recorder.RecordAsync("SafetyStateChanged", e, e.OccurredAt, ct: ct);
    }

    private async Task OnDeviceStatusChangedAsync(DeviceStatusChangedEventArgs e, CancellationToken ct)
    {
        await _recorder.RecordAsync("DeviceStatusChanged", e, e.OccurredAt, ct: ct);
    }

    public void Dispose()
    {
        // 取消订阅所有事件
        _eventBus.Unsubscribe<LineSpeedChangedEventArgs>(OnLineSpeedChangedAsync);
        _eventBus.Unsubscribe<ParcelCreatedEventArgs>(OnParcelCreatedAsync);
        _eventBus.Unsubscribe<ParcelDivertedEventArgs>(OnParcelDivertedAsync);
        _eventBus.Unsubscribe<OriginCartChangedEventArgs>(OnOriginCartChangedAsync);
        _eventBus.Unsubscribe<CartAtChuteChangedEventArgs>(OnCartAtChuteChangedAsync);
        _eventBus.Unsubscribe<CartLayoutChangedEventArgs>(OnCartLayoutChangedAsync);
        _eventBus.Unsubscribe<LineRunStateChangedEventArgs>(OnLineRunStateChangedAsync);
        _eventBus.Unsubscribe<SafetyStateChangedEventArgs>(OnSafetyStateChangedAsync);
        _eventBus.Unsubscribe<DeviceStatusChangedEventArgs>(OnDeviceStatusChangedAsync);

        _logger.LogInformation("Recording event subscriber disposed");
    }
}

using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Observability;
using ZakYip.NarrowBeltDiverterSorter.Observability.Recording;

namespace ZakYip.NarrowBeltDiverterSorter.Simulation.Replay;

/// <summary>
/// 录制回放运行器实现
/// 从录制会话中读取事件并重放到事件总线
/// </summary>
public class RecordingReplayRunner : IRecordingReplayRunner
{
    private readonly IEventRecordingManager _recordingManager;
    private readonly IEventBus _eventBus;
    private readonly ILogger<RecordingReplayRunner> _logger;
    private readonly string _recordingsBaseDirectory;

    public RecordingReplayRunner(
        IEventRecordingManager recordingManager,
        IEventBus eventBus,
        ILogger<RecordingReplayRunner> logger)
        : this(recordingManager, eventBus, "recordings", logger)
    {
    }

    public RecordingReplayRunner(
        IEventRecordingManager recordingManager,
        IEventBus eventBus,
        string recordingsBaseDirectory,
        ILogger<RecordingReplayRunner> logger)
    {
        _recordingManager = recordingManager ?? throw new ArgumentNullException(nameof(recordingManager));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _recordingsBaseDirectory = recordingsBaseDirectory ?? throw new ArgumentNullException(nameof(recordingsBaseDirectory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task ReplayAsync(Guid sessionId, ReplayConfiguration configuration, CancellationToken ct = default)
    {
        _logger.LogInformation("Starting replay of session {SessionId} with mode {Mode}", 
            sessionId, configuration.Mode);

        // 1. 获取会话信息
        var session = await _recordingManager.GetSessionAsync(sessionId, ct);
        if (session == null)
        {
            throw new InvalidOperationException($"Recording session {sessionId} not found");
        }

        if (!session.IsCompleted)
        {
            throw new InvalidOperationException($"Recording session {sessionId} is not completed");
        }

        // 2. 读取事件流
        var events = await LoadEventsAsync(sessionId, ct);
        
        if (events.Count == 0)
        {
            _logger.LogWarning("No events found in session {SessionId}", sessionId);
            return;
        }

        _logger.LogInformation("Loaded {EventCount} events from session {SessionId}", 
            events.Count, sessionId);

        // 3. 按时间戳排序
        var sortedEvents = events.OrderBy(e => e.Timestamp).ToList();

        // 4. 回放事件
        await ReplayEventsAsync(sortedEvents, configuration, ct);

        _logger.LogInformation("Completed replay of session {SessionId}", sessionId);
    }

    private async Task<List<RecordedEventEnvelope>> LoadEventsAsync(Guid sessionId, CancellationToken ct)
    {
        var events = new List<RecordedEventEnvelope>();
        var eventsFile = Path.Combine(_recordingsBaseDirectory, sessionId.ToString(), "events.ndjson");

        if (!File.Exists(eventsFile))
        {
            return events;
        }

        var lines = await File.ReadAllLinesAsync(eventsFile, ct);
        
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            try
            {
                var envelope = JsonSerializer.Deserialize<RecordedEventEnvelope>(line);
                events.Add(envelope);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize event: {Line}", line);
            }
        }

        return events;
    }

    private async Task ReplayEventsAsync(
        List<RecordedEventEnvelope> events,
        ReplayConfiguration configuration,
        CancellationToken ct)
    {
        DateTimeOffset? previousTimestamp = null;

        for (int i = 0; i < events.Count; i++)
        {
            ct.ThrowIfCancellationRequested();

            var eventEnvelope = events[i];

            // 计算延迟
            if (previousTimestamp.HasValue)
            {
                var originalDelay = eventEnvelope.Timestamp - previousTimestamp.Value;
                var replayDelay = CalculateReplayDelay(originalDelay, configuration);

                if (replayDelay > TimeSpan.Zero)
                {
                    await Task.Delay(replayDelay, ct);
                }
            }

            // 发布事件到事件总线
            await PublishEventAsync(eventEnvelope, ct);

            previousTimestamp = eventEnvelope.Timestamp;

            if ((i + 1) % 100 == 0)
            {
                _logger.LogDebug("Replayed {Count}/{Total} events", i + 1, events.Count);
            }
        }
    }

    private TimeSpan CalculateReplayDelay(TimeSpan originalDelay, ReplayConfiguration configuration)
    {
        return configuration.Mode switch
        {
            ReplayMode.OriginalSpeed => originalDelay,
            ReplayMode.Accelerated => TimeSpan.FromMilliseconds(originalDelay.TotalMilliseconds / configuration.SpeedFactor),
            ReplayMode.FixedInterval => TimeSpan.FromMilliseconds(configuration.FixedIntervalMs),
            _ => originalDelay
        };
    }

    private async Task PublishEventAsync(RecordedEventEnvelope envelope, CancellationToken ct)
    {
        try
        {
            // 根据事件类型反序列化并发布
            // 注意：这里需要根据事件类型重新构造强类型事件
            // 为简化实现，我们先记录事件类型，实际应该根据类型映射到具体的事件类
            
            switch (envelope.EventType)
            {
                case "LineSpeedChanged":
                    var speedEvent = JsonSerializer.Deserialize<LineSpeedChangedEventArgs>(envelope.PayloadJson);
                    if (speedEvent != null)
                    {
                        await _eventBus.PublishAsync(speedEvent, ct);
                    }
                    break;

                case "ParcelCreated":
                    var parcelCreatedEvent = JsonSerializer.Deserialize<ParcelCreatedEventArgs>(envelope.PayloadJson);
                    if (parcelCreatedEvent != null)
                    {
                        await _eventBus.PublishAsync(parcelCreatedEvent, ct);
                    }
                    break;

                case "ParcelDiverted":
                    var parcelDivertedEvent = JsonSerializer.Deserialize<ParcelDivertedEventArgs>(envelope.PayloadJson);
                    if (parcelDivertedEvent != null)
                    {
                        await _eventBus.PublishAsync(parcelDivertedEvent, ct);
                    }
                    break;

                case "OriginCartChanged":
                    var originCartEvent = JsonSerializer.Deserialize<OriginCartChangedEventArgs>(envelope.PayloadJson);
                    if (originCartEvent != null)
                    {
                        await _eventBus.PublishAsync(originCartEvent, ct);
                    }
                    break;

                case "CartAtChuteChanged":
                    var cartAtChuteEvent = JsonSerializer.Deserialize<CartAtChuteChangedEventArgs>(envelope.PayloadJson);
                    if (cartAtChuteEvent != null)
                    {
                        await _eventBus.PublishAsync(cartAtChuteEvent, ct);
                    }
                    break;

                case "CartLayoutChanged":
                    var cartLayoutEvent = JsonSerializer.Deserialize<CartLayoutChangedEventArgs>(envelope.PayloadJson);
                    if (cartLayoutEvent != null)
                    {
                        await _eventBus.PublishAsync(cartLayoutEvent, ct);
                    }
                    break;

                case "LineRunStateChanged":
                    var lineRunStateEvent = JsonSerializer.Deserialize<Observability.Events.LineRunStateChangedEventArgs>(envelope.PayloadJson);
                    if (lineRunStateEvent != null)
                    {
                        await _eventBus.PublishAsync(lineRunStateEvent, ct);
                    }
                    break;

                case "SafetyStateChanged":
                    var safetyStateEvent = JsonSerializer.Deserialize<Observability.Events.SafetyStateChangedEventArgs>(envelope.PayloadJson);
                    if (safetyStateEvent != null)
                    {
                        await _eventBus.PublishAsync(safetyStateEvent, ct);
                    }
                    break;

                case "DeviceStatusChanged":
                    var deviceStatusEvent = JsonSerializer.Deserialize<DeviceStatusChangedEventArgs>(envelope.PayloadJson);
                    if (deviceStatusEvent != null)
                    {
                        await _eventBus.PublishAsync(deviceStatusEvent, ct);
                    }
                    break;

                default:
                    _logger.LogWarning("Unknown event type: {EventType}", envelope.EventType);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event {EventType}", envelope.EventType);
        }
    }
}

using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Communication.Upstream;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Upstream;

namespace ZakYip.NarrowBeltDiverterSorter.Communication.Upstream;

/// <summary>
/// 规则引擎客户端包装器，负责发布状态变更事件和收集指标
/// </summary>
public class ObservableSortingRuleEngineClient : ISortingRuleEngineClient
{
    private readonly ISortingRuleEngineClient _innerClient;
    private readonly IEventBus? _eventBus;
    private readonly ILogger<ObservableSortingRuleEngineClient> _logger;
    private readonly string _mode;
    private readonly string? _connectionAddress;

    // 指标统计
    private long _totalRequests = 0;
    private long _successfulResponses = 0;
    private long _failedResponses = 0;
    private readonly object _metricsLock = new();
    private readonly List<double> _latencySamples = new();
    private const int MaxLatencySamples = 100; // 保留最近100个样本用于计算平均值
    private string? _lastError;
    private DateTimeOffset? _lastErrorAt;

    public ObservableSortingRuleEngineClient(
        ISortingRuleEngineClient innerClient,
        string mode,
        string? connectionAddress,
        IEventBus? eventBus,
        ILogger<ObservableSortingRuleEngineClient> logger)
    {
        _innerClient = innerClient ?? throw new ArgumentNullException(nameof(innerClient));
        _mode = mode ?? throw new ArgumentNullException(nameof(mode));
        _connectionAddress = connectionAddress;
        _eventBus = eventBus;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public bool IsConnected => _innerClient.IsConnected;

    public event EventHandler<UpstreamContracts.Models.SortingResultMessage>? SortingResultReceived
    {
        add => _innerClient.SortingResultReceived += value;
        remove => _innerClient.SortingResultReceived -= value;
    }

    public event EventHandler<UpstreamContracts.Models.ChuteAssignmentNotificationEventArgs>? ChuteAssignmentReceived
    {
        add => _innerClient.ChuteAssignmentReceived += value;
        remove => _innerClient.ChuteAssignmentReceived -= value;
    }

    public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        PublishStatusChange(UpstreamConnectionStatus.Connecting);

        var result = await _innerClient.ConnectAsync(cancellationToken);

        var status = result 
            ? UpstreamConnectionStatus.Connected 
            : UpstreamConnectionStatus.Error;

        PublishStatusChange(status);

        return result;
    }

    public async Task DisconnectAsync()
    {
        await _innerClient.DisconnectAsync();
        PublishStatusChange(UpstreamConnectionStatus.Disconnected);
    }

    public Task<bool> SendParcelCreatedAsync(UpstreamContracts.Models.ParcelCreatedMessage message, CancellationToken cancellationToken = default)
    {
        return TrackRequestAsync(() => _innerClient.SendParcelCreatedAsync(message, cancellationToken));
    }

    public Task<bool> SendDwsDataAsync(UpstreamContracts.Models.DwsDataMessage message, CancellationToken cancellationToken = default)
    {
        return TrackRequestAsync(() => _innerClient.SendDwsDataAsync(message, cancellationToken));
    }

    public Task<bool> SendSortingResultAsync(UpstreamContracts.Models.SortingResultMessage message, CancellationToken cancellationToken = default)
    {
        return TrackRequestAsync(() => _innerClient.SendSortingResultAsync(message, cancellationToken));
    }

    public void Dispose()
    {
        _innerClient.Dispose();
    }

    private async Task<bool> TrackRequestAsync(Func<Task<bool>> operation)
    {
        var startTime = DateTimeOffset.Now;
        bool success = false;
        string? errorMessage = null;

        try
        {
            Interlocked.Increment(ref _totalRequests);
            success = await operation();

            if (success)
            {
                Interlocked.Increment(ref _successfulResponses);
            }
            else
            {
                Interlocked.Increment(ref _failedResponses);
                errorMessage = "操作返回 false";
            }
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref _failedResponses);
            errorMessage = ex.Message;
            throw;
        }
        finally
        {
            var latency = (DateTimeOffset.Now - startTime).TotalMilliseconds;
            
            lock (_metricsLock)
            {
                _latencySamples.Add(latency);
                if (_latencySamples.Count > MaxLatencySamples)
                {
                    _latencySamples.RemoveAt(0);
                }

                if (!success && errorMessage != null)
                {
                    _lastError = errorMessage;
                    _lastErrorAt = DateTimeOffset.Now;
                }
            }

            PublishMetrics();
        }

        return success;
    }

    private void PublishMetrics()
    {
        if (_eventBus == null)
            return;

        try
        {
            double avgLatency;
            string? lastError;
            DateTimeOffset? lastErrorAt;

            lock (_metricsLock)
            {
                avgLatency = _latencySamples.Count > 0 ? _latencySamples.Average() : 0;
                lastError = _lastError;
                lastErrorAt = _lastErrorAt;
            }

            var eventArgs = new UpstreamMetricsEventArgs
            {
                TotalRequests = _totalRequests,
                SuccessfulResponses = _successfulResponses,
                FailedResponses = _failedResponses,
                AverageLatencyMs = avgLatency,
                LastError = lastError,
                LastErrorAt = lastErrorAt,
                Timestamp = DateTimeOffset.Now
            };

            _ = _eventBus.PublishAsync(eventArgs, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发布上游规则引擎指标事件失败");
        }
    }

    private void PublishStatusChange(UpstreamConnectionStatus status)
    {
        if (_eventBus == null)
            return;

        try
        {
            var eventArgs = new UpstreamRuleEngineStatusChangedEventArgs
            {
                Mode = _mode,
                Status = status,
                ConnectionAddress = _connectionAddress,
                Timestamp = DateTimeOffset.Now
            };

            _ = _eventBus.PublishAsync(eventArgs, CancellationToken.None);

            _logger.LogDebug("发布上游规则引擎状态变更事件: Mode={Mode}, Status={Status}", _mode, status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发布上游规则引擎状态变更事件失败");
        }
    }
}

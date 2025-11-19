using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Communication.Upstream;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Observability.Events;
using ZakYip.NarrowBeltDiverterSorter.Observability.LiveView;

namespace ZakYip.NarrowBeltDiverterSorter.Communication.Upstream;

/// <summary>
/// 规则引擎客户端包装器，负责发布状态变更事件
/// </summary>
public class ObservableSortingRuleEngineClient : ISortingRuleEngineClient
{
    private readonly ISortingRuleEngineClient _innerClient;
    private readonly IEventBus? _eventBus;
    private readonly ILogger<ObservableSortingRuleEngineClient> _logger;
    private readonly string _mode;
    private readonly string? _connectionAddress;

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
        return _innerClient.SendParcelCreatedAsync(message, cancellationToken);
    }

    public Task<bool> SendDwsDataAsync(UpstreamContracts.Models.DwsDataMessage message, CancellationToken cancellationToken = default)
    {
        return _innerClient.SendDwsDataAsync(message, cancellationToken);
    }

    public Task<bool> SendSortingResultAsync(UpstreamContracts.Models.SortingResultMessage message, CancellationToken cancellationToken = default)
    {
        return _innerClient.SendSortingResultAsync(message, cancellationToken);
    }

    public void Dispose()
    {
        _innerClient.Dispose();
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
                Timestamp = DateTimeOffset.UtcNow
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

using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.UpstreamContracts.Models;

namespace ZakYip.NarrowBeltDiverterSorter.Communication.Upstream;

/// <summary>
/// 基于 TCP 协议的分拣规则引擎客户端实现（骨架/预留）
/// TODO: 实现完整的 TCP 连接和消息交互逻辑
/// </summary>
public class TcpSortingRuleEngineClient : ISortingRuleEngineClient
{
    private readonly ILogger<TcpSortingRuleEngineClient> _logger;
    private readonly TcpConfiguration _config;

    public TcpSortingRuleEngineClient(
        ILogger<TcpSortingRuleEngineClient> logger,
        TcpConfiguration config)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        ConnectionState = UpstreamConnectionState.Disconnected;
    }

    public UpstreamConnectionState ConnectionState { get; private set; }

    public ValueTask ConnectAsync(CancellationToken ct = default)
    {
        _logger.LogWarning("TCP 客户端尚未实现，Host={Host}, Port={Port}", _config.Host, _config.Port);
        ConnectionState = UpstreamConnectionState.Error;
        return ValueTask.CompletedTask;
    }

    public ValueTask DisconnectAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("TCP 客户端断开连接（未实现）");
        ConnectionState = UpstreamConnectionState.Disconnected;
        return ValueTask.CompletedTask;
    }

    public ValueTask PublishParcelCreatedAsync(ParcelCreatedMessage message, CancellationToken ct = default)
    {
        _logger.LogWarning("TCP 客户端尚未实现 - 忽略包裹创建消息 ParcelId={ParcelId}", message.ParcelId);
        return ValueTask.CompletedTask;
    }

    public ValueTask PublishDwsDataAsync(DwsDataMessage message, CancellationToken ct = default)
    {
        _logger.LogWarning("TCP 客户端尚未实现 - 忽略 DWS 数据消息 ParcelId={ParcelId}", message.ParcelId);
        return ValueTask.CompletedTask;
    }

    public ValueTask PublishSortingResultAsync(SortingResultMessage message, CancellationToken ct = default)
    {
        _logger.LogWarning("TCP 客户端尚未实现 - 忽略分拣结果消息 ParcelId={ParcelId}", message.ParcelId);
        return ValueTask.CompletedTask;
    }

    public ValueTask SubscribeToSortingRequestsAsync(Func<long, ValueTask<int>> onSortingRequest, CancellationToken ct = default)
    {
        _logger.LogWarning("TCP 客户端尚未实现 - 不订阅分拣请求");
        return ValueTask.CompletedTask;
    }
}

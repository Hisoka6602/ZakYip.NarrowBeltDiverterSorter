using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.UpstreamContracts.Models;

namespace ZakYip.NarrowBeltDiverterSorter.Communication.Upstream;

/// <summary>
/// 禁用的分拣规则引擎客户端
/// 用于单机仿真模式，不执行任何实际的网络操作，只打印日志
/// </summary>
public class DisabledSortingRuleEngineClient : ISortingRuleEngineClient
{
    private readonly ILogger<DisabledSortingRuleEngineClient> _logger;
    private readonly int _defaultChuteNumber;

    public DisabledSortingRuleEngineClient(
        ILogger<DisabledSortingRuleEngineClient> logger,
        int defaultChuteNumber = 1)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _defaultChuteNumber = defaultChuteNumber;
        ConnectionState = UpstreamConnectionState.Disconnected;
    }

    public UpstreamConnectionState ConnectionState { get; private set; }

    public ValueTask ConnectAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("上游规则引擎适配器: Disabled（已禁用）- 不连接到任何外部服务");
        ConnectionState = UpstreamConnectionState.Disconnected;
        return ValueTask.CompletedTask;
    }

    public ValueTask DisconnectAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("上游规则引擎适配器: Disabled - 断开连接（无操作）");
        ConnectionState = UpstreamConnectionState.Disconnected;
        return ValueTask.CompletedTask;
    }

    public ValueTask PublishParcelCreatedAsync(ParcelCreatedMessage message, CancellationToken ct = default)
    {
        _logger.LogDebug("上游规则引擎适配器: Disabled - 忽略包裹创建消息 ParcelId={ParcelId}", message.ParcelId);
        return ValueTask.CompletedTask;
    }

    public ValueTask PublishDwsDataAsync(DwsDataMessage message, CancellationToken ct = default)
    {
        _logger.LogDebug("上游规则引擎适配器: Disabled - 忽略 DWS 数据消息 ParcelId={ParcelId}", message.ParcelId);
        return ValueTask.CompletedTask;
    }

    public ValueTask PublishSortingResultAsync(SortingResultMessage message, CancellationToken ct = default)
    {
        _logger.LogDebug("上游规则引擎适配器: Disabled - 忽略分拣结果消息 ParcelId={ParcelId}, ChuteNumber={ChuteNumber}, Success={Success}",
            message.ParcelId, message.ChuteNumber, message.Success);
        return ValueTask.CompletedTask;
    }

    public ValueTask SubscribeToSortingRequestsAsync(Func<long, ValueTask<int>> onSortingRequest, CancellationToken ct = default)
    {
        _logger.LogDebug("上游规则引擎适配器: Disabled - 不订阅分拣请求，始终使用默认格口 {DefaultChuteNumber}", _defaultChuteNumber);
        return ValueTask.CompletedTask;
    }
}

using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.UpstreamContracts.Models;

namespace ZakYip.NarrowBeltDiverterSorter.Communication.Upstream;

/// <summary>
/// 禁用的规则引擎客户端（no-op 实现）
/// 用于单机仿真模式，不进行任何实际的网络通信
/// </summary>
public class DisabledSortingRuleEngineClient : ISortingRuleEngineClient
{
    private readonly ILogger<DisabledSortingRuleEngineClient> _logger;

    public DisabledSortingRuleEngineClient(ILogger<DisabledSortingRuleEngineClient> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogInformation("上游规则引擎适配器: Disabled（已禁用）");
    }

    /// <inheritdoc/>
    public bool IsConnected => false;

    /// <inheritdoc/>
#pragma warning disable CS0067 // 事件 'SortingResultReceived' 未使用：此事件为 ISortingRuleEngineClient 接口的必需成员。此实现为禁用状态的客户端，不会触发事件。
    public event EventHandler<SortingResultMessage>? SortingResultReceived;
#pragma warning restore CS0067

    /// <inheritdoc/>
    public Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("上游规则引擎连接被禁用，跳过连接");
        return Task.FromResult(false);
    }

    /// <inheritdoc/>
    public Task DisconnectAsync()
    {
        _logger.LogDebug("上游规则引擎连接被禁用，跳过断开连接");
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<bool> SendParcelCreatedAsync(ParcelCreatedMessage message, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("上游规则引擎已禁用，跳过发送包裹创建消息: ParcelId={ParcelId}", message.ParcelId);
        return Task.FromResult(false);
    }

    /// <inheritdoc/>
    public Task<bool> SendDwsDataAsync(DwsDataMessage message, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("上游规则引擎已禁用，跳过发送 DWS 数据消息: ParcelId={ParcelId}", message.ParcelId);
        return Task.FromResult(false);
    }

    /// <inheritdoc/>
    public Task<bool> SendSortingResultAsync(SortingResultMessage message, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("上游规则引擎已禁用，跳过发送分拣结果消息: ParcelId={ParcelId}", message.ParcelId);
        return Task.FromResult(false);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        // No resources to dispose
    }
}

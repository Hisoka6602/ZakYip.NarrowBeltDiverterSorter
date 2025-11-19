using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.UpstreamContracts.Models;

namespace ZakYip.NarrowBeltDiverterSorter.Communication.Upstream;

/// <summary>
/// TCP 协议的规则引擎客户端实现（骨架）
/// </summary>
/// <remarks>
/// 这是一个预留的骨架实现，暂未实现具体的 TCP 通信逻辑
/// </remarks>
public class TcpSortingRuleEngineClient : ISortingRuleEngineClient
{
    private readonly TcpOptions _options;
    private readonly ILogger<TcpSortingRuleEngineClient> _logger;
    private bool _disposed;

    public TcpSortingRuleEngineClient(
        TcpOptions options,
        ILogger<TcpSortingRuleEngineClient> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public bool IsConnected => false;

    /// <inheritdoc/>
    public event EventHandler<SortingResultMessage>? SortingResultReceived;

    /// <inheritdoc/>
    public Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("TCP 客户端尚未实现，无法连接到 {Host}:{Port}", _options.Host, _options.Port);
        return Task.FromResult(false);
    }

    /// <inheritdoc/>
    public Task DisconnectAsync()
    {
        _logger.LogDebug("TCP 客户端断开连接（骨架实现）");
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<bool> SendParcelCreatedAsync(ParcelCreatedMessage message, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("TCP 客户端尚未实现，无法发送包裹创建消息: ParcelId={ParcelId}", message.ParcelId);
        return Task.FromResult(false);
    }

    /// <inheritdoc/>
    public Task<bool> SendDwsDataAsync(DwsDataMessage message, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("TCP 客户端尚未实现，无法发送 DWS 数据消息: ParcelId={ParcelId}", message.ParcelId);
        return Task.FromResult(false);
    }

    /// <inheritdoc/>
    public Task<bool> SendSortingResultAsync(SortingResultMessage message, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("TCP 客户端尚未实现，无法发送分拣结果消息: ParcelId={ParcelId}", message.ParcelId);
        return Task.FromResult(false);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
            return;

        // TODO: 释放 TCP 连接资源
        _disposed = true;
    }
}

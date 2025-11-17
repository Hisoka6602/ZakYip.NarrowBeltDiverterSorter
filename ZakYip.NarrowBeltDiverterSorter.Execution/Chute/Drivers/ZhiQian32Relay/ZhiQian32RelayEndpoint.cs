using Microsoft.Extensions.Logging;

namespace ZakYip.NarrowBeltDiverterSorter.Execution.Chute.Drivers.ZhiQian32Relay;

/// <summary>
/// 智嵌32路网络继电器端点实现
/// 实现 IChuteIoEndpoint 接口，内部持有 ZhiQian32RelayClient 进行TCP通信
/// </summary>
public sealed class ZhiQian32RelayEndpoint : IChuteIoEndpoint, IDisposable
{
    private readonly ILogger<ZhiQian32RelayEndpoint> _logger;
    private readonly ZhiQian32RelayClient _client;
    private readonly int _maxChannelCount;
    private bool _disposed;

    /// <inheritdoc/>
    public string EndpointKey { get; }

    /// <summary>
    /// 创建智嵌32路网络继电器端点实例
    /// </summary>
    /// <param name="endpointKey">端点唯一键</param>
    /// <param name="ipAddress">目标IP地址</param>
    /// <param name="port">TCP端口</param>
    /// <param name="maxChannelCount">最大通道数（默认32）</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="clientLogger">客户端日志记录器</param>
    public ZhiQian32RelayEndpoint(
        string endpointKey,
        string ipAddress,
        int port,
        int maxChannelCount,
        ILogger<ZhiQian32RelayEndpoint> logger,
        ILogger<ZhiQian32RelayClient> clientLogger)
    {
        EndpointKey = endpointKey ?? throw new ArgumentNullException(nameof(endpointKey));
        _maxChannelCount = maxChannelCount;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _client = new ZhiQian32RelayClient(ipAddress, port, clientLogger);

        _logger.LogInformation(
            "[智嵌继电器端点] 创建端点 {EndpointKey}，目标 {IpAddress}:{Port}，最大通道数 {MaxChannelCount}",
            EndpointKey,
            ipAddress,
            port,
            maxChannelCount);
    }

    /// <inheritdoc/>
    public async ValueTask SetChannelAsync(int channelIndex, bool isOn, CancellationToken ct = default)
    {
        if (channelIndex < 1 || channelIndex > _maxChannelCount)
        {
            _logger.LogError(
                "[智嵌继电器端点] 端点 {EndpointKey} 通道索引 {ChannelIndex} 超出范围 (1..{MaxChannelCount})，拒绝发送",
                EndpointKey,
                channelIndex,
                _maxChannelCount);
            return;
        }

        try
        {
            _logger.LogInformation(
                "[智嵌继电器端点] 端点 {EndpointKey} 设置通道 {ChannelIndex} 状态为 {State}",
                EndpointKey,
                channelIndex,
                isOn ? "开" : "关");

            await _client.SetChannelAsync(channelIndex, isOn, ct);

            _logger.LogInformation(
                "[智嵌继电器端点] 端点 {EndpointKey} 通道 {ChannelIndex} 设置成功",
                EndpointKey,
                channelIndex);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[智嵌继电器端点] 端点 {EndpointKey} 设置通道 {ChannelIndex} 失败",
                EndpointKey,
                channelIndex);

            // 捕获异常并记录，但不抛出，避免影响整个 Host 进程
            // 外部调用者可以通过日志了解失败情况
        }
    }

    /// <inheritdoc/>
    public async ValueTask SetAllAsync(bool isOn, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation(
                "[智嵌继电器端点] 端点 {EndpointKey} 设置所有通道 (1..{MaxChannelCount}) 状态为 {State}",
                EndpointKey,
                _maxChannelCount,
                isOn ? "开" : "关");

            await _client.SetAllChannelsAsync(isOn, ct);

            _logger.LogInformation(
                "[智嵌继电器端点] 端点 {EndpointKey} 批量设置成功",
                EndpointKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[智嵌继电器端点] 端点 {EndpointKey} 批量设置失败",
                EndpointKey);

            // 捕获异常并记录，但不抛出，避免影响整个 Host 进程
            // 外部调用者可以通过日志了解失败情况
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _client?.Dispose();
        _disposed = true;

        _logger.LogInformation(
            "[智嵌继电器端点] 端点 {EndpointKey} 已释放资源",
            EndpointKey);
    }
}

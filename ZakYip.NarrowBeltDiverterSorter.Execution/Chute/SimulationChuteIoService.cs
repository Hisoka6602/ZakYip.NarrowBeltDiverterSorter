using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;

namespace ZakYip.NarrowBeltDiverterSorter.Execution.Chute;

/// <summary>
/// 模拟格口 IO 服务实现
/// 内部维护格口到端点通道的映射关系，按照配置做转发
/// </summary>
public class SimulationChuteIoService : IChuteIoService
{
    private readonly ILogger<SimulationChuteIoService> _logger;
    private readonly Dictionary<long, (IChuteIoEndpoint endpoint, int channelIndex)> _chuteMapping;
    private readonly List<IChuteIoEndpoint> _endpoints;

    /// <summary>
    /// 创建模拟格口 IO 服务实例
    /// </summary>
    /// <param name="endpoints">端点列表</param>
    /// <param name="chuteMapping">格口到端点通道的映射</param>
    /// <param name="logger">日志记录器</param>
    public SimulationChuteIoService(
        IEnumerable<IChuteIoEndpoint> endpoints,
        Dictionary<long, (IChuteIoEndpoint endpoint, int channelIndex)> chuteMapping,
        ILogger<SimulationChuteIoService> logger)
    {
        _endpoints = endpoints?.ToList() ?? throw new ArgumentNullException(nameof(endpoints));
        _chuteMapping = chuteMapping ?? throw new ArgumentNullException(nameof(chuteMapping));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async ValueTask OpenAsync(long chuteId, CancellationToken ct = default)
    {
        if (!_chuteMapping.TryGetValue(chuteId, out var mapping))
        {
            _logger.LogWarning(
                "[模拟格口IO服务] 格口 {ChuteId} 未配置映射关系",
                chuteId);
            return;
        }

        _logger.LogInformation(
            "[模拟格口IO服务] 打开格口 {ChuteId} (端点={EndpointKey}, 通道={ChannelIndex})",
            chuteId,
            mapping.endpoint.EndpointKey,
            mapping.channelIndex);

        await mapping.endpoint.SetChannelAsync(mapping.channelIndex, true, ct);
    }

    /// <inheritdoc/>
    public async ValueTask CloseAsync(long chuteId, CancellationToken ct = default)
    {
        if (!_chuteMapping.TryGetValue(chuteId, out var mapping))
        {
            _logger.LogWarning(
                "[模拟格口IO服务] 格口 {ChuteId} 未配置映射关系",
                chuteId);
            return;
        }

        _logger.LogInformation(
            "[模拟格口IO服务] 关闭格口 {ChuteId} (端点={EndpointKey}, 通道={ChannelIndex})",
            chuteId,
            mapping.endpoint.EndpointKey,
            mapping.channelIndex);

        await mapping.endpoint.SetChannelAsync(mapping.channelIndex, false, ct);
    }

    /// <inheritdoc/>
    public async ValueTask CloseAllAsync(CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[模拟格口IO服务] 关闭所有格口 (共 {EndpointCount} 个端点)",
            _endpoints.Count);

        foreach (var endpoint in _endpoints)
        {
            await endpoint.SetAllAsync(false, ct);
        }
    }
}

using Microsoft.Extensions.Logging;

namespace ZakYip.NarrowBeltDiverterSorter.Execution.Chute;

/// <summary>
/// 模拟格口 IO 端点实现
/// 只记录日志，不连硬件
/// </summary>
public class SimulationChuteIoEndpoint : IChuteIoEndpoint
{
    private readonly ILogger<SimulationChuteIoEndpoint> _logger;
    private readonly int _maxChannelCount;

    /// <inheritdoc/>
    public string EndpointKey { get; }

    /// <summary>
    /// 创建模拟格口 IO 端点实例
    /// </summary>
    /// <param name="endpointKey">端点唯一键</param>
    /// <param name="maxChannelCount">最大通道数</param>
    /// <param name="logger">日志记录器</param>
    public SimulationChuteIoEndpoint(
        string endpointKey,
        int maxChannelCount,
        ILogger<SimulationChuteIoEndpoint> logger)
    {
        EndpointKey = endpointKey ?? throw new ArgumentNullException(nameof(endpointKey));
        _maxChannelCount = maxChannelCount;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public ValueTask SetChannelAsync(int channelIndex, bool isOn, CancellationToken ct = default)
    {
        if (channelIndex < 1 || channelIndex > _maxChannelCount)
        {
            _logger.LogWarning(
                "[模拟格口IO端点] 端点 {EndpointKey} 通道索引 {ChannelIndex} 超出范围 (1..{MaxChannelCount})",
                EndpointKey,
                channelIndex,
                _maxChannelCount);
            return ValueTask.CompletedTask;
        }

        _logger.LogInformation(
            "[模拟格口IO端点] 端点 {EndpointKey} 通道 {ChannelIndex} 状态设置为 {State}",
            EndpointKey,
            channelIndex,
            isOn ? "开" : "关");

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    public ValueTask SetAllAsync(bool isOn, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[模拟格口IO端点] 端点 {EndpointKey} 所有通道 (1..{MaxChannelCount}) 状态设置为 {State}",
            EndpointKey,
            _maxChannelCount,
            isOn ? "开" : "关");

        return ValueTask.CompletedTask;
    }
}

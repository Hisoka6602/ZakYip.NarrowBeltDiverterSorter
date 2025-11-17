using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Sorting;

namespace ZakYip.NarrowBeltDiverterSorter.Simulation.Fakes;

/// <summary>
/// 仿真格口执行器
/// 用于仿真环境下的格口操作，输出日志以便观察和调试
/// </summary>
public sealed class SimulationChuteActuator : IChuteActuator
{
    private readonly ILogger<SimulationChuteActuator> _logger;
    private readonly ChuteIoMappingOptions _options;
    private readonly IChuteTransmitterPort? _chuteTransmitterPort;

    /// <summary>
    /// 构造函数（用于纯日志模式）
    /// </summary>
    public SimulationChuteActuator(
        IOptions<ChuteIoMappingOptions> options,
        ILogger<SimulationChuteActuator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _chuteTransmitterPort = null;
    }

    /// <summary>
    /// 构造函数（用于与现有仿真 Port 集成）
    /// </summary>
    public SimulationChuteActuator(
        IOptions<ChuteIoMappingOptions> options,
        ILogger<SimulationChuteActuator> logger,
        IChuteTransmitterPort chuteTransmitterPort)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _chuteTransmitterPort = chuteTransmitterPort ?? throw new ArgumentNullException(nameof(chuteTransmitterPort));
    }

    /// <inheritdoc/>
    public async ValueTask TriggerAsync(long chuteId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[仿真格口] 触发格口 {ChuteId}", chuteId);

        // 如果提供了 IChuteTransmitterPort，则调用它以保持现有仿真可视化
        if (_chuteTransmitterPort != null)
        {
            var openDuration = TimeSpan.FromMilliseconds(_options.PulseDurationMilliseconds);
            await _chuteTransmitterPort.OpenWindowAsync(new ChuteId(chuteId), openDuration, cancellationToken);
        }
    }

    /// <inheritdoc/>
    public async ValueTask CloseAsync(long chuteId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[仿真格口] 关闭格口 {ChuteId}", chuteId);

        // 如果提供了 IChuteTransmitterPort，则调用它
        if (_chuteTransmitterPort != null)
        {
            await _chuteTransmitterPort.ForceCloseAsync(new ChuteId(chuteId), cancellationToken);
        }
    }

    /// <inheritdoc/>
    public async ValueTask CloseAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[仿真格口] 关闭所有格口");

        // 如果提供了 IChuteTransmitterPort，关闭所有格口
        if (_chuteTransmitterPort != null)
        {
            var tasks = new List<Task>();
            foreach (var chuteIdChannel in _options.ChuteIdToIoChannel)
            {
                tasks.Add(_chuteTransmitterPort.ForceCloseAsync(new ChuteId(chuteIdChannel.Key), cancellationToken));
            }
            await Task.WhenAll(tasks);
        }
    }
}

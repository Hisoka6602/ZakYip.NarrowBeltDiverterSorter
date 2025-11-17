using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Sorting;

namespace ZakYip.NarrowBeltDiverterSorter.Execution.Chute;

/// <summary>
/// 现场总线格口执行器
/// 用于真实硬件环境，通过 IChuteTransmitterPort 与现场总线通信
/// </summary>
public sealed class FieldBusChuteActuator : IChuteActuator
{
    private readonly ILogger<FieldBusChuteActuator> _logger;
    private readonly ChuteIoMappingOptions _options;
    private readonly IChuteTransmitterPort _chuteTransmitterPort;

    public FieldBusChuteActuator(
        IOptions<ChuteIoMappingOptions> options,
        ILogger<FieldBusChuteActuator> logger,
        IChuteTransmitterPort chuteTransmitterPort)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _chuteTransmitterPort = chuteTransmitterPort ?? throw new ArgumentNullException(nameof(chuteTransmitterPort));
    }

    /// <inheritdoc/>
    public async ValueTask TriggerAsync(long chuteId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[现场总线格口] 触发格口 {ChuteId}", chuteId);

        var openDuration = TimeSpan.FromMilliseconds(_options.PulseDurationMilliseconds);
        await _chuteTransmitterPort.OpenWindowAsync(new ChuteId(chuteId), openDuration, cancellationToken);
    }

    /// <inheritdoc/>
    public async ValueTask CloseAsync(long chuteId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[现场总线格口] 关闭格口 {ChuteId}", chuteId);

        await _chuteTransmitterPort.ForceCloseAsync(new ChuteId(chuteId), cancellationToken);
    }

    /// <inheritdoc/>
    public async ValueTask CloseAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[现场总线格口] 关闭所有格口");

        var tasks = new List<Task>();
        foreach (var chuteIdChannel in _options.ChuteIdToIoChannel)
        {
            tasks.Add(_chuteTransmitterPort.ForceCloseAsync(new ChuteId(chuteIdChannel.Key), cancellationToken));
        }
        await Task.WhenAll(tasks);
    }
}

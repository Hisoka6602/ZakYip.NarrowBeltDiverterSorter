using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Sorting;

namespace ZakYip.NarrowBeltDiverterSorter.Execution.Chute;

/// <summary>
/// 将 IChuteTransmitterPort 适配为 IChuteIoService
/// 这个适配器允许现有的基于 IChuteTransmitterPort 的代码通过新的 IChuteIoService 接口工作
/// 注意：此适配器不支持 openDuration 参数，默认使用 300ms
/// </summary>
public class ChuteTransmitterPortAdapter : IChuteIoService
{
    private readonly IChuteTransmitterPort _transmitterPort;
    private readonly IChuteConfigProvider _chuteConfigProvider;
    private readonly ILogger<ChuteTransmitterPortAdapter> _logger;
    private readonly TimeSpan _defaultOpenDuration;

    /// <summary>
    /// 创建适配器实例
    /// </summary>
    /// <param name="transmitterPort">底层传输器端口</param>
    /// <param name="chuteConfigProvider">格口配置提供者（用于获取默认开启时长）</param>
    /// <param name="logger">日志记录器</param>
    public ChuteTransmitterPortAdapter(
        IChuteTransmitterPort transmitterPort,
        IChuteConfigProvider chuteConfigProvider,
        ILogger<ChuteTransmitterPortAdapter> logger)
    {
        _transmitterPort = transmitterPort ?? throw new ArgumentNullException(nameof(transmitterPort));
        _chuteConfigProvider = chuteConfigProvider ?? throw new ArgumentNullException(nameof(chuteConfigProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _defaultOpenDuration = TimeSpan.FromMilliseconds(300);
    }

    /// <inheritdoc/>
    public async ValueTask OpenAsync(long chuteId, CancellationToken ct = default)
    {
        var chuteIdObj = new ChuteId(chuteId);
        
        // 尝试从配置获取开启时长，如果失败使用默认值
        var openDuration = _defaultOpenDuration;
        try
        {
            var config = _chuteConfigProvider.GetConfig(chuteIdObj);
            if (config != null && config.MaxOpenDuration != TimeSpan.Zero)
            {
                openDuration = config.MaxOpenDuration;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "无法获取格口 {ChuteId} 的配置，使用默认开启时长 {Duration}ms", 
                chuteId, openDuration.TotalMilliseconds);
        }

        await _transmitterPort.OpenWindowAsync(chuteIdObj, openDuration, ct);
    }

    /// <inheritdoc/>
    public async ValueTask CloseAsync(long chuteId, CancellationToken ct = default)
    {
        var chuteIdObj = new ChuteId(chuteId);
        await _transmitterPort.ForceCloseAsync(chuteIdObj, ct);
    }

    /// <inheritdoc/>
    public async ValueTask CloseAllAsync(CancellationToken ct = default)
    {
        var allChutes = _chuteConfigProvider.GetAllConfigs();
        
        _logger.LogInformation("关闭全部 {Count} 个格口", allChutes.Count);

        var closeTasks = new List<ValueTask>();
        foreach (var chute in allChutes)
        {
            closeTasks.Add(CloseAsync(chute.ChuteId.Value, ct));
        }

        // Wait for all close operations to complete
        foreach (var task in closeTasks)
        {
            await task;
        }
    }
}

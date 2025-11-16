using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Sorting;

namespace ZakYip.NarrowBeltDiverterSorter.Execution.Sorting;

/// <summary>
/// 格口安全控制服务实现（面向真实硬件）
/// </summary>
public class ChuteSafetyService : IChuteSafetyService
{
    private readonly IChuteTransmitterPort _chuteTransmitterPort;
    private readonly IChuteConfigProvider _chuteConfigProvider;
    private readonly ILogger<ChuteSafetyService> _logger;

    public ChuteSafetyService(
        IChuteTransmitterPort chuteTransmitterPort,
        IChuteConfigProvider chuteConfigProvider,
        ILogger<ChuteSafetyService> logger)
    {
        _chuteTransmitterPort = chuteTransmitterPort ?? throw new ArgumentNullException(nameof(chuteTransmitterPort));
        _chuteConfigProvider = chuteConfigProvider ?? throw new ArgumentNullException(nameof(chuteConfigProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task CloseAllChutesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var allChutes = _chuteConfigProvider.GetAllConfigs();
            _logger.LogInformation("安全控制: 正在关闭全部 {Count} 个格口发信器...", allChutes.Count);

            var closeTasks = new List<Task>();
            foreach (var chute in allChutes)
            {
                closeTasks.Add(CloseChuteSafelyAsync(chute.ChuteId, cancellationToken));
            }

            await Task.WhenAll(closeTasks);
            
            _logger.LogInformation("安全控制: 已关闭全部格口发信器");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "安全控制: 关闭全部格口发信器时发生异常");
            // Don't rethrow - we want to continue even if some chutes fail to close
        }
    }

    private async Task CloseChuteSafelyAsync(ChuteId chuteId, CancellationToken cancellationToken)
    {
        try
        {
            await _chuteTransmitterPort.ForceCloseAsync(chuteId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "安全控制: 关闭格口 {ChuteId} 时失败", chuteId.Value);
            // Continue with other chutes
        }
    }
}

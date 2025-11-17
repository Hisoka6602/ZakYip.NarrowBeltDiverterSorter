using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Sorting;

namespace ZakYip.NarrowBeltDiverterSorter.Execution.Sorting;

/// <summary>
/// 格口安全控制服务实现
/// 优先使用 IChuteIoService（支持多IP端点），回退到 IChuteTransmitterPort（向后兼容）
/// </summary>
public class ChuteSafetyService : IChuteSafetyService
{
    private readonly IChuteIoService? _chuteIoService;
    private readonly IChuteTransmitterPort? _chuteTransmitterPort;
    private readonly IChuteConfigProvider? _chuteConfigProvider;
    private readonly ILogger<ChuteSafetyService> _logger;

    public ChuteSafetyService(
        ILogger<ChuteSafetyService> logger,
        IChuteIoService? chuteIoService = null,
        IChuteTransmitterPort? chuteTransmitterPort = null,
        IChuteConfigProvider? chuteConfigProvider = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _chuteIoService = chuteIoService;
        _chuteTransmitterPort = chuteTransmitterPort;
        _chuteConfigProvider = chuteConfigProvider;

        // At least one implementation should be provided
        if (_chuteIoService == null && _chuteTransmitterPort == null)
        {
            _logger.LogWarning("安全控制: 既没有 IChuteIoService 也没有 IChuteTransmitterPort，CloseAllChutesAsync 将无操作");
        }
    }

    /// <inheritdoc/>
    public async Task CloseAllChutesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // 优先使用 IChuteIoService（支持多IP端点）
            if (_chuteIoService != null)
            {
                _logger.LogInformation("安全控制: 正在通过 IChuteIoService 关闭全部格口...");
                await _chuteIoService.CloseAllAsync(cancellationToken);
                _logger.LogInformation("安全控制: 已通过 IChuteIoService 关闭全部格口");
                return;
            }

            // 回退到传统的 IChuteTransmitterPort 方式
            if (_chuteTransmitterPort != null && _chuteConfigProvider != null)
            {
                var allChutes = _chuteConfigProvider.GetAllConfigs();
                _logger.LogInformation("安全控制: 正在通过 IChuteTransmitterPort 关闭全部 {Count} 个格口发信器...", allChutes.Count);

                var closeTasks = new List<Task>();
                foreach (var chute in allChutes)
                {
                    closeTasks.Add(CloseChuteSafelyAsync(chute.ChuteId, cancellationToken));
                }

                await Task.WhenAll(closeTasks);
                
                _logger.LogInformation("安全控制: 已通过 IChuteTransmitterPort 关闭全部格口发信器");
                return;
            }

            _logger.LogWarning("安全控制: 无可用的格口关闭实现");
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
            if (_chuteTransmitterPort != null)
            {
                await _chuteTransmitterPort.ForceCloseAsync(chuteId, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "安全控制: 关闭格口 {ChuteId} 时失败", chuteId.Value);
            // Continue with other chutes
        }
    }
}

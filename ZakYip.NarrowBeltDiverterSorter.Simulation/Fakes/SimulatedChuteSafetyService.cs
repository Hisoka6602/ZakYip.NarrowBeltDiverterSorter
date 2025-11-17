using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Sorting;

namespace ZakYip.NarrowBeltDiverterSorter.Simulation.Fakes;

/// <summary>
/// 仿真格口安全控制服务（使用 IChuteActuator）
/// </summary>
public class SimulatedChuteSafetyService : IChuteSafetyService
{
    private readonly IChuteActuator _chuteActuator;
    private readonly ILogger<SimulatedChuteSafetyService> _logger;

    public SimulatedChuteSafetyService(
        IChuteActuator chuteActuator,
        ILogger<SimulatedChuteSafetyService> logger)
    {
        _chuteActuator = chuteActuator ?? throw new ArgumentNullException(nameof(chuteActuator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task CloseAllChutesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("安全控制: 正在关闭全部格口发信器...");

            await _chuteActuator.CloseAllAsync(cancellationToken);
            
            _logger.LogInformation("安全控制: 已关闭全部格口发信器");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "安全控制: 关闭全部格口发信器时发生异常");
            // Don't rethrow - we want to continue even if some chutes fail to close
        }
    }
}

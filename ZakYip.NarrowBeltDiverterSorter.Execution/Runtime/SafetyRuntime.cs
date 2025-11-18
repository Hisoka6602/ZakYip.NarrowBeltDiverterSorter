using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Runtime;

namespace ZakYip.NarrowBeltDiverterSorter.Execution.Runtime;

/// <summary>
/// 安全控制运行时实现
/// 负责在启动时和停止时关闭全部格口发信器
/// </summary>
public class SafetyRuntime : ISafetyRuntime
{
    private readonly IChuteSafetyService _chuteSafetyService;
    private readonly ILogger<SafetyRuntime> _logger;

    public SafetyRuntime(
        IChuteSafetyService chuteSafetyService,
        ILogger<SafetyRuntime> logger)
    {
        _chuteSafetyService = chuteSafetyService ?? throw new ArgumentNullException(nameof(chuteSafetyService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("安全控制运行时已启动");
        
        // 启动前关闭全部格口发信器
        _logger.LogInformation("安全控制: 启动前关闭全部格口发信器");
        
        try
        {
            await _chuteSafetyService.CloseAllChutesAsync(cancellationToken);
            _logger.LogInformation("安全控制: 启动前已关闭全部格口发信器");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "安全控制: 启动前关闭全部格口发信器时发生异常");
            // Don't throw - allow the application to continue starting
        }

        // 等待取消信号
        try
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // 正常停止
        }

        // 停止前关闭全部格口发信器
        _logger.LogInformation("安全控制: 停止前关闭全部格口发信器");
        
        try
        {
            // Use a timeout to ensure we don't block shutdown indefinitely
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await _chuteSafetyService.CloseAllChutesAsync(cts.Token);
            
            _logger.LogInformation("安全控制: 停止前已关闭全部格口发信器");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "安全控制: 停止前关闭全部格口发信器时发生异常");
            // Continue shutdown even if this fails
        }

        _logger.LogInformation("安全控制运行时已停止");
    }
}

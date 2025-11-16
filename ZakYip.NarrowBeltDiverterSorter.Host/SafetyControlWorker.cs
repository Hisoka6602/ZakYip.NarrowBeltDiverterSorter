using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;

namespace ZakYip.NarrowBeltDiverterSorter.Host;

/// <summary>
/// 安全控制工作器
/// 负责在启动时和停止时关闭全部格口发信器
/// </summary>
public class SafetyControlWorker : IHostedService
{
    private readonly IChuteSafetyService _chuteSafetyService;
    private readonly ILogger<SafetyControlWorker> _logger;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;

    public SafetyControlWorker(
        IChuteSafetyService chuteSafetyService,
        ILogger<SafetyControlWorker> logger,
        IHostApplicationLifetime hostApplicationLifetime)
    {
        _chuteSafetyService = chuteSafetyService ?? throw new ArgumentNullException(nameof(chuteSafetyService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _hostApplicationLifetime = hostApplicationLifetime ?? throw new ArgumentNullException(nameof(hostApplicationLifetime));
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
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

        // Register shutdown callback
        _hostApplicationLifetime.ApplicationStopping.Register(() =>
        {
            try
            {
                _logger.LogInformation("安全控制: 停止前关闭全部格口发信器");
                
                // Use a timeout to ensure we don't block shutdown indefinitely
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                _chuteSafetyService.CloseAllChutesAsync(cts.Token).GetAwaiter().GetResult();
                
                _logger.LogInformation("安全控制: 停止前已关闭全部格口发信器");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "安全控制: 停止前关闭全部格口发信器时发生异常");
                // Continue shutdown even if this fails
            }
        });
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        // Actual cleanup is done in ApplicationStopping callback
        return Task.CompletedTask;
    }
}

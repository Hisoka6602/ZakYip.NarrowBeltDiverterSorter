using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Runtime;

namespace ZakYip.NarrowBeltDiverterSorter.Host;

/// <summary>
/// 安全控制工作器
/// 作为 ASP.NET IHostedService 的薄壳，将安全控制逻辑委托给 ISafetyRuntime
/// </summary>
public class SafetyControlWorker : IHostedService
{
    private readonly ISafetyRuntime _runtime;
    private readonly ILogger<SafetyControlWorker> _logger;
    private Task? _runtimeTask;
    private CancellationTokenSource? _cts;

    public SafetyControlWorker(
        ISafetyRuntime runtime,
        ILogger<SafetyControlWorker> logger)
    {
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("安全控制工作器已启动，将启动运行时");
        
        _cts = new CancellationTokenSource();
        _runtimeTask = _runtime.RunAsync(_cts.Token);
        
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("安全控制工作器正在停止");
        
        if (_cts != null)
        {
            _cts.Cancel();
        }
        
        if (_runtimeTask != null)
        {
            try
            {
                await _runtimeTask;
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
        }
        
        _cts?.Dispose();
        _logger.LogInformation("安全控制工作器已停止");
    }
}

using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Feeding;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Runtime;

namespace ZakYip.NarrowBeltDiverterSorter.Host;

/// <summary>
/// 包裹路由工作器
/// 作为 ASP.NET BackgroundService 的薄壳，将路由逻辑委托给 IParcelRoutingRuntime
/// </summary>
public class ParcelRoutingWorker : BackgroundService
{
    private readonly IParcelRoutingRuntime _runtime;
    private readonly ILogger<ParcelRoutingWorker> _logger;

    public ParcelRoutingWorker(
        IParcelRoutingRuntime runtime,
        ILogger<ParcelRoutingWorker> logger)
    {
        _runtime = runtime;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("包裹路由工作器已启动，将启动运行时");
        return _runtime.RunAsync(stoppingToken);
    }

    /// <summary>
    /// 处理包裹创建事件
    /// 此方法应该由事件订阅机制调用
    /// </summary>
    /// <param name="eventArgs">包裹创建事件参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task HandleParcelCreatedAsync(
        ParcelCreatedFromInfeedEventArgs eventArgs,
        CancellationToken cancellationToken = default)
    {
        // 委托给运行时处理
        if (_runtime is ZakYip.NarrowBeltDiverterSorter.Execution.Runtime.ParcelRoutingRuntime runtime)
        {
            await runtime.HandleParcelCreatedAsync(eventArgs, cancellationToken);
        }
    }
}

using Microsoft.Extensions.Options;
using ZakYip.NarrowBeltDiverterSorter.Core.Configuration;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Runtime;

namespace ZakYip.NarrowBeltDiverterSorter.Host;

/// <summary>
/// 主线控制工作器
/// 作为 ASP.NET BackgroundService 的薄壳，将控制逻辑委托给 IMainLineRuntime
/// </summary>
public class MainLineControlWorker : BackgroundService
{
    private readonly IMainLineRuntime _runtime;
    private readonly ILogger<MainLineControlWorker> _logger;

    public MainLineControlWorker(
        IMainLineRuntime runtime,
        ILogger<MainLineControlWorker> logger)
    {
        _runtime = runtime;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("主线控制工作器已启动，将启动运行时");
        return _runtime.RunAsync(stoppingToken);
    }
}

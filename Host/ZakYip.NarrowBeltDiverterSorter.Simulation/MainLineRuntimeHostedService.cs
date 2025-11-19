using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Runtime;

namespace ZakYip.NarrowBeltDiverterSorter.Simulation;

/// <summary>
/// 主线运行时托管服务
/// 在 ASP.NET 生命周期中启动 IMainLineRuntime
/// </summary>
public class MainLineRuntimeHostedService : BackgroundService
{
    private readonly IMainLineRuntime _runtime;
    private readonly ILogger<MainLineRuntimeHostedService> _logger;

    public MainLineRuntimeHostedService(
        IMainLineRuntime runtime,
        ILogger<MainLineRuntimeHostedService> logger)
    {
        _runtime = runtime;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("主线运行时托管服务已启动");
        return _runtime.RunAsync(stoppingToken);
    }
}

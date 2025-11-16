using Microsoft.Extensions.Options;
using ZakYip.NarrowBeltDiverterSorter.Communication;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Application;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Parcels;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.MainLine;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Tracking;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Feeding;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Sorting;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Carts;
using ZakYip.NarrowBeltDiverterSorter.Drivers.MainLine;
using ZakYip.NarrowBeltDiverterSorter.Drivers.Cart;
using ZakYip.NarrowBeltDiverterSorter.Drivers.Chute;
using ZakYip.NarrowBeltDiverterSorter.Execution.MainLine;
using ZakYip.NarrowBeltDiverterSorter.Execution.Feeding;
using ZakYip.NarrowBeltDiverterSorter.Execution.Sorting;
using ZakYip.NarrowBeltDiverterSorter.Observability;
using ZakYip.NarrowBeltDiverterSorter.Ingress.Chute;
using ZakYip.NarrowBeltDiverterSorter.Infrastructure;
using ZakYip.NarrowBeltDiverterSorter.Infrastructure.Configuration;
using ZakYip.NarrowBeltDiverterSorter.Communication.Upstream;
using ZakYip.NarrowBeltDiverterSorter.Host;

// ============================================================================
// 解析启动模式参数
// ============================================================================

var startupConfig = StartupModeConfiguration.ParseFromArgs(args);
Console.WriteLine($"启动模式: {startupConfig.GetModeDescription()}");

var builder = Host.CreateApplicationBuilder(args);

// 注册启动模式配置为单例
builder.Services.AddSingleton(startupConfig);

// ============================================================================
// 配置选项
// ============================================================================

// 注册配置仓储
builder.Services.AddSingleton<IMainLineOptionsRepository, LiteDbMainLineOptionsRepository>();
builder.Services.AddSingleton<IInfeedLayoutOptionsRepository, LiteDbInfeedLayoutOptionsRepository>();
builder.Services.AddSingleton<IChuteConfigRepository, LiteDbChuteConfigRepository>();
builder.Services.AddSingleton<IUpstreamConnectionOptionsRepository, LiteDbUpstreamConnectionOptionsRepository>();

// 配置上游分拣系统API选项（保留用于非核心配置）
builder.Services.Configure<UpstreamSortingApiOptions>(
    builder.Configuration.GetSection(UpstreamSortingApiOptions.SectionName));

// 从数据库加载主线控制选项
builder.Services.AddSingleton<IOptions<MainLineControlOptions>>(sp =>
{
    var repo = sp.GetRequiredService<IMainLineOptionsRepository>();
    var options = repo.LoadAsync().GetAwaiter().GetResult();
    return Options.Create(options);
});

// 从数据库加载入口布局选项  
builder.Services.AddSingleton<IOptions<InfeedLayoutOptions>>(sp =>
{
    var repo = sp.GetRequiredService<IInfeedLayoutOptionsRepository>();
    var options = repo.LoadAsync().GetAwaiter().GetResult();
    return Options.Create(options);
});

// 配置格口IO监视器选项
builder.Services.Configure<ChuteIoMonitorConfiguration>(
    builder.Configuration.GetSection("ChuteIoMonitor"));

// 配置小车参数寄存器
builder.Services.Configure<CartParameterRegisterConfiguration>(
    builder.Configuration.GetSection("CartParameterRegisters"));

// 配置格口映射
builder.Services.Configure<ChuteMappingConfiguration>(
    builder.Configuration.GetSection("ChuteMapping"));

// 配置现场总线客户端
builder.Services.Configure<FieldBusClientConfiguration>(
    builder.Configuration.GetSection("FieldBus"));

// ============================================================================
// 注册事件总线 (Observability)
// ============================================================================

builder.Services.AddSingleton<IEventBus, InMemoryEventBus>();

// ============================================================================
// 注册配置存储 (Infrastructure)
// ============================================================================

builder.Services.AddSingleton<IConfigStore, LiteDbConfigStore>();

// ============================================================================
// 注册上游客户端
// ============================================================================

// 注册HttpClient for UpstreamSortingApiClient
builder.Services.AddHttpClient<IUpstreamSortingApiClient, UpstreamSortingApiClient>((serviceProvider, client) =>
{
    var repo = serviceProvider.GetRequiredService<IUpstreamConnectionOptionsRepository>();
    var options = repo.LoadAsync().GetAwaiter().GetResult();
    
    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(options.RequestTimeoutSeconds);
    
    // 如果配置了认证令牌，添加到请求头
    if (!string.IsNullOrWhiteSpace(options.AuthToken))
    {
        client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.AuthToken);
    }
});

// ============================================================================
// 注册现场总线和驱动
// ============================================================================

// 注册现场总线客户端
builder.Services.AddSingleton<IFieldBusClient, FieldBusClient>();

// 注册主线驱动和反馈端口（单例）
builder.Services.AddSingleton<RemaMainLineDrive>();
builder.Services.AddSingleton<IMainLineDrivePort>(sp => sp.GetRequiredService<RemaMainLineDrive>());
builder.Services.AddSingleton<IMainLineFeedbackPort>(sp => sp.GetRequiredService<RemaMainLineDrive>());

// 注册小车参数驱动
builder.Services.AddSingleton<ICartParameterPort, CartParameterDriver>();

// 注册格口发信器驱动
builder.Services.AddSingleton<IChuteTransmitterPort, ChuteTransmitterDriver>();

// 注册传感器端口（这些需要具体实现，这里使用占位符）
// TODO: 实现具体的传感器端口
// builder.Services.AddSingleton<IOriginSensorPort, OriginSensorPortImplementation>();
// builder.Services.AddSingleton<IInfeedSensorPort, InfeedSensorPortImplementation>();
// builder.Services.AddSingleton<IInfeedConveyorPort, InfeedConveyorPortImplementation>();

// ============================================================================
// 注册领域服务
// ============================================================================

// 注册格口配置提供者（从数据库加载）
builder.Services.AddSingleton<IChuteConfigProvider, RepositoryBackedChuteConfigProvider>();

// 注册小车环构建器
builder.Services.AddSingleton<ICartRingBuilder, CartRingBuilder>();

// 注册小车位置跟踪器
builder.Services.AddSingleton<ICartPositionTracker, CartPositionTracker>();

// 注册包裹生命周期服务（单例，内存存储）
builder.Services.AddSingleton<IParcelLifecycleService, ParcelLifecycleService>();

// 注册小车生命周期服务
builder.Services.AddSingleton<ICartLifecycleService, CartLifecycleService>();

// 注册包裹装载计划器
builder.Services.AddSingleton<IParcelLoadPlanner, ParcelLoadPlanner>();

// 注册分拣计划器
builder.Services.AddSingleton<ISortingPlanner, SortingPlanner>();

// 注册主线控制服务和速度提供者
builder.Services.AddSingleton<IMainLineControlService, MainLineControlService>();
builder.Services.AddSingleton<IMainLineSpeedProvider, MainLineSpeedProvider>();
builder.Services.AddSingleton<IMainLineStabilityProvider, MainLineStabilityProvider>();

// ============================================================================
// 注册健康检查
// ============================================================================

builder.Services.AddHealthChecks()
    .AddCheck<SystemHealthCheck>("system");

// ============================================================================
// 根据启动模式注册后台工作器
// ============================================================================

// 主线控制工作器（所有模式都需要）
if (startupConfig.ShouldStartMainLineControl())
{
    builder.Services.AddHostedService<MainLineControlWorker>();
}

// 原点传感器监视器（所有模式都需要）
if (startupConfig.ShouldStartOriginSensorMonitor())
{
    // builder.Services.AddHostedService<OriginSensorMonitorWorker>();
    // TODO: 当 IOriginSensorPort 实现后启用
}

// 入口传感器监视器（bringup-infeed 及以上模式）
if (startupConfig.ShouldStartInfeedSensorMonitor())
{
    // builder.Services.AddHostedService<InfeedSensorMonitorWorker>();
    // TODO: 当 IInfeedSensorPort 实现后启用
}

// 包裹装载协调器（bringup-infeed 及以上模式）
if (startupConfig.ShouldStartParcelLoadCoordinator())
{
    builder.Services.AddHostedService<ParcelLoadCoordinatorWorker>();
}

// 分拣执行工作器（bringup-chutes 及以上模式）
if (startupConfig.ShouldStartSortingExecutionWorker())
{
    builder.Services.AddHostedService<SortingExecutionWorker>();
}

// 格口IO监视器（bringup-chutes 及以上模式）
if (startupConfig.ShouldStartChuteIoMonitor())
{
    // builder.Services.AddHostedService<ChuteIoMonitorWorker>();
    // TODO: 当 FieldBusClient 实现后启用
}

// 包裹路由工作器（仅 normal 模式，上游相关）
if (startupConfig.ShouldStartParcelRoutingWorker())
{
    builder.Services.AddHostedService<ParcelRoutingWorker>();
}

// 注册占位符工作器（可以移除）
builder.Services.AddHostedService<Worker>();

var host = builder.Build();

// 输出启动信息
var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("=== 系统启动模式: {Mode} ===", startupConfig.GetModeDescription());
logger.LogInformation("已启动服务:");
if (startupConfig.ShouldStartMainLineControl())
    logger.LogInformation("  - 主线控制工作器 (MainLineControlWorker)");
if (startupConfig.ShouldStartOriginSensorMonitor())
    logger.LogInformation("  - 原点传感器监控 (OriginSensorMonitor) [待实现]");
if (startupConfig.ShouldStartInfeedSensorMonitor())
    logger.LogInformation("  - 入口传感器监控 (InfeedSensorMonitor) [待实现]");
if (startupConfig.ShouldStartParcelLoadCoordinator())
    logger.LogInformation("  - 包裹装载协调器 (ParcelLoadCoordinator)");
if (startupConfig.ShouldStartSortingExecutionWorker())
    logger.LogInformation("  - 分拣执行工作器 (SortingExecutionWorker)");
if (startupConfig.ShouldStartChuteIoMonitor())
    logger.LogInformation("  - 格口IO监视器 (ChuteIoMonitor) [待实现]");
if (startupConfig.ShouldStartParcelRoutingWorker())
    logger.LogInformation("  - 包裹路由工作器 (ParcelRoutingWorker)");

host.Run();

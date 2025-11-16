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
using ZakYip.NarrowBeltDiverterSorter.Host;

var builder = Host.CreateApplicationBuilder(args);

// ============================================================================
// 配置选项
// ============================================================================

// 配置上游分拣系统API选项
builder.Services.Configure<UpstreamSortingApiOptions>(
    builder.Configuration.GetSection(UpstreamSortingApiOptions.SectionName));

// 配置主线控制选项
builder.Services.Configure<MainLineControlOptions>(
    builder.Configuration.GetSection("MainLineControl"));

// 配置分拣执行选项
builder.Services.Configure<SortingExecutionOptions>(
    builder.Configuration.GetSection("SortingExecution"));

// 配置分拣计划器选项
builder.Services.Configure<SortingPlannerOptions>(
    builder.Configuration.GetSection("SortingPlanner"));

// 配置入口布局选项
builder.Services.Configure<InfeedLayoutOptions>(
    builder.Configuration.GetSection("InfeedLayout"));

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
// 注册上游客户端
// ============================================================================

// 注册HttpClient for UpstreamSortingApiClient
builder.Services.AddHttpClient<IUpstreamSortingApiClient, UpstreamSortingApiClient>((serviceProvider, client) =>
{
    var options = builder.Configuration
        .GetSection(UpstreamSortingApiOptions.SectionName)
        .Get<UpstreamSortingApiOptions>() ?? new UpstreamSortingApiOptions();
    
    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
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

// 注册小车环构建器
builder.Services.AddSingleton<ICartRingBuilder, CartRingBuilder>();

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

// ============================================================================
// 注册健康检查
// ============================================================================

builder.Services.AddHealthChecks()
    .AddCheck<SystemHealthCheck>("system");

// ============================================================================
// 注册后台工作器
// ============================================================================

// 注册主线控制工作器
builder.Services.AddHostedService<MainLineControlWorker>();

// 注册包裹路由工作器
builder.Services.AddHostedService<ParcelRoutingWorker>();

// 注册分拣执行工作器
builder.Services.AddHostedService<SortingExecutionWorker>();

// 注册传感器监视器工作器（当传感器端口实现后启用）
// builder.Services.AddHostedService<OriginSensorMonitorWorker>();
// builder.Services.AddHostedService<InfeedSensorMonitorWorker>();
// builder.Services.AddHostedService<ChuteIoMonitorWorker>();

// 注册占位符工作器（可以移除）
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();

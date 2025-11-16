using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Communication;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Application;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Carts;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Feeding;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.MainLine;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Parcels;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Sorting;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Tracking;
using ZakYip.NarrowBeltDiverterSorter.Execution.Feeding;
using ZakYip.NarrowBeltDiverterSorter.Execution.MainLine;
using ZakYip.NarrowBeltDiverterSorter.Execution.Sorting;
using ZakYip.NarrowBeltDiverterSorter.Host;
using ZakYip.NarrowBeltDiverterSorter.Ingress.Infeed;
using ZakYip.NarrowBeltDiverterSorter.Ingress.Origin;
using ZakYip.NarrowBeltDiverterSorter.Simulation;
using ZakYip.NarrowBeltDiverterSorter.Simulation.Fakes;

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine("════════════════════════════════════════");
Console.WriteLine("  窄带分拣机仿真系统 (Narrow Belt Sorter Simulation)");
Console.WriteLine("════════════════════════════════════════\n");

var builder = Host.CreateApplicationBuilder(args);

// ============================================================================
// 配置仿真参数
// ============================================================================

var simulationConfig = new SimulationConfiguration
{
    NumberOfCarts = 20,
    CartSpacingMm = 500m,
    NumberOfChutes = 10,
    ForceEjectChuteId = 10,
    MainLineSpeedMmPerSec = 1000.0,
    InfeedConveyorSpeedMmPerSec = 1000.0,
    InfeedToDropDistanceMm = 2000m,
    ParcelGenerationIntervalSeconds = 2.0,
    SimulationDurationSeconds = 60
};

Console.WriteLine($"仿真配置:");
Console.WriteLine($"  小车数量: {simulationConfig.NumberOfCarts}");
Console.WriteLine($"  小车节距: {simulationConfig.CartSpacingMm} mm");
Console.WriteLine($"  格口数量: {simulationConfig.NumberOfChutes}");
Console.WriteLine($"  强排口: 格口 {simulationConfig.ForceEjectChuteId}");
Console.WriteLine($"  主线速度: {simulationConfig.MainLineSpeedMmPerSec} mm/s");
Console.WriteLine($"  包裹生成间隔: {simulationConfig.ParcelGenerationIntervalSeconds} 秒");
Console.WriteLine($"  仿真时长: {simulationConfig.SimulationDurationSeconds} 秒\n");

builder.Services.AddSingleton(simulationConfig);

// ============================================================================
// 配置选项
// ============================================================================

builder.Services.Configure<MainLineControlOptions>(options =>
{
    options.TargetSpeedMmps = (decimal)simulationConfig.MainLineSpeedMmPerSec;
    options.LoopPeriod = TimeSpan.FromMilliseconds(100);
    options.StableDeadbandMmps = 50m;
});

// Register SortingPlannerOptions directly as singleton
builder.Services.AddSingleton(new SortingPlannerOptions
{
    CartSpacingMm = simulationConfig.CartSpacingMm
});

builder.Services.Configure<SortingExecutionOptions>(options =>
{
    options.ExecutionPeriod = TimeSpan.FromMilliseconds(100);
    options.PlanningHorizon = TimeSpan.FromSeconds(5);
});

// Register InfeedLayoutOptions directly as singleton
builder.Services.AddSingleton(new InfeedLayoutOptions
{
    InfeedToMainLineDistanceMm = simulationConfig.InfeedToDropDistanceMm,
    TimeToleranceMs = 50,
    CartOffsetCalibration = 0
});

// ============================================================================
// 注册 Fake 硬件实现
// ============================================================================

var fakeMainLineDrive = new FakeMainLineDrivePort();
builder.Services.AddSingleton(fakeMainLineDrive);
builder.Services.AddSingleton<IMainLineDrivePort>(fakeMainLineDrive);

var fakeMainLineFeedback = new FakeMainLineFeedbackPort(fakeMainLineDrive);
builder.Services.AddSingleton(fakeMainLineFeedback);
builder.Services.AddSingleton<IMainLineFeedbackPort>(fakeMainLineFeedback);

var fakeFieldBus = new FakeFieldBusClient();
builder.Services.AddSingleton(fakeFieldBus);
builder.Services.AddSingleton<IFieldBusClient>(fakeFieldBus);

var fakeInfeedSensor = new FakeInfeedSensorPort();
builder.Services.AddSingleton(fakeInfeedSensor);
builder.Services.AddSingleton<IInfeedSensorPort>(fakeInfeedSensor);

var fakeOriginSensor = new FakeOriginSensorPort();
builder.Services.AddSingleton(fakeOriginSensor);
builder.Services.AddSingleton<IOriginSensorPort>(fakeOriginSensor);

var fakeInfeedConveyor = new FakeInfeedConveyorPort();
builder.Services.AddSingleton(fakeInfeedConveyor);
builder.Services.AddSingleton<IInfeedConveyorPort>(fakeInfeedConveyor);

var fakeChuteTransmitter = new FakeChuteTransmitterPort();
builder.Services.AddSingleton<IChuteTransmitterPort>(fakeChuteTransmitter);

builder.Services.AddSingleton<IUpstreamSortingApiClient, FakeUpstreamSortingApiClient>();

// ============================================================================
// 注册领域服务
// ============================================================================

builder.Services.AddSingleton<ICartRingBuilder, CartRingBuilder>();
builder.Services.AddSingleton<IParcelLifecycleService, ParcelLifecycleService>();
builder.Services.AddSingleton<ICartLifecycleService, CartLifecycleService>();
builder.Services.AddSingleton<IParcelLoadPlanner, ParcelLoadPlanner>();
builder.Services.AddSingleton<ISortingPlanner, SortingPlanner>();
builder.Services.AddSingleton<IMainLineControlService, MainLineControlService>();
builder.Services.AddSingleton<IMainLineSpeedProvider, MainLineSpeedProvider>();
builder.Services.AddSingleton<ICartPositionTracker, CartPositionTracker>();
builder.Services.AddSingleton<IChuteConfigProvider>(sp =>
{
    var provider = new ChuteConfigProvider();
    for (int i = 1; i <= simulationConfig.NumberOfChutes; i++)
    {
        provider.AddOrUpdate(new ZakYip.NarrowBeltDiverterSorter.Core.Domain.ChuteConfig
        {
            ChuteId = new ZakYip.NarrowBeltDiverterSorter.Core.Domain.ChuteId(i),
            IsEnabled = true,
            IsForceEject = (i == simulationConfig.ForceEjectChuteId),
            CartOffsetFromOrigin = i * 2, // 假设每个格口间隔2个小车位置
            MaxOpenDuration = TimeSpan.FromMilliseconds(300)
        });
    }
    return provider;
});

builder.Services.AddSingleton<ParcelLoadCoordinator>();

// ============================================================================
// 注册 Ingress 监视器
// ============================================================================

builder.Services.AddSingleton<OriginSensorMonitor>();
builder.Services.AddSingleton<InfeedSensorMonitor>();

// ============================================================================
// 注册后台工作器
// ============================================================================

builder.Services.AddHostedService<MainLineControlWorker>();
builder.Services.AddHostedService<ParcelRoutingWorker>();
builder.Services.AddHostedService<SortingExecutionWorker>();
builder.Services.AddHostedService<SimulationOrchestrator>();
builder.Services.AddHostedService<ParcelGeneratorWorker>();
builder.Services.AddHostedService<CartMovementSimulator>();

// ============================================================================
// 配置日志
// ============================================================================

builder.Logging.ClearProviders();
builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Critical;
});
builder.Logging.SetMinimumLevel(LogLevel.Warning);

var host = builder.Build();

Console.WriteLine("正在启动仿真...\n");

await host.RunAsync();

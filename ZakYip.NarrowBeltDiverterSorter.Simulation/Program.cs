using System.CommandLine;
using System.Text.Json;
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
using ZakYip.NarrowBeltDiverterSorter.Infrastructure.Configuration;
using ZakYip.NarrowBeltDiverterSorter.Ingress.Infeed;
using ZakYip.NarrowBeltDiverterSorter.Ingress.Origin;
using ZakYip.NarrowBeltDiverterSorter.Simulation;
using ZakYip.NarrowBeltDiverterSorter.Simulation.Fakes;

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine("════════════════════════════════════════");
Console.WriteLine("  窄带分拣机仿真系统 (Narrow Belt Sorter Simulation)");
Console.WriteLine("════════════════════════════════════════\n");

// ============================================================================
// 定义命令行参数
// ============================================================================

var scenarioOption = new Option<string?>(
    name: "--scenario",
    description: "场景名称，例如 narrowbelt-e2e");

var parcelCountOption = new Option<int>(
    name: "--parcel-count",
    getDefaultValue: () => 50,
    description: "本次仿真包裹数量");

var outputOption = new Option<string?>(
    name: "--output",
    description: "报告输出路径，例如 simulation-report.json");

var resetConfigOption = new Option<bool>(
    name: "--reset-config",
    getDefaultValue: () => false,
    description: "仿真前清空 LiteDB 配置并写入默认配置");

var rootCommand = new RootCommand("窄带分拣机仿真系统")
{
    scenarioOption,
    parcelCountOption,
    outputOption,
    resetConfigOption
};

rootCommand.SetHandler(async (scenario, parcelCount, output, resetConfig) =>
{
    await RunSimulationAsync(scenario, parcelCount, output, resetConfig);
}, scenarioOption, parcelCountOption, outputOption, resetConfigOption);

return await rootCommand.InvokeAsync(args);

static async Task RunSimulationAsync(string? scenario, int parcelCount, string? output, bool resetConfig)
{
    // 如果指定了 E2E 场景，运行 E2E 模式
    if (scenario == "narrowbelt-e2e")
    {
        await RunE2EScenarioAsync(parcelCount, output, resetConfig);
    }
    else
    {
        // 否则运行传统仿真模式
        await RunTraditionalSimulationAsync();
    }
}

static async Task RunE2EScenarioAsync(int parcelCount, string? outputPath, bool resetConfig)
{
    Console.WriteLine($"═══ 运行 E2E 场景 ═══");
    Console.WriteLine($"包裹数量: {parcelCount}");
    Console.WriteLine($"输出路径: {outputPath ?? "(未指定)"}");
    Console.WriteLine($"重置配置: {(resetConfig ? "是" : "否")}\n");

    var builder = Host.CreateApplicationBuilder();

    // ============================================================================
    // 配置 LiteDB
    // ============================================================================

    var dbPath = Path.Combine(Environment.CurrentDirectory, "simulation.db");
    if (resetConfig && File.Exists(dbPath))
    {
        Console.WriteLine($"删除现有配置数据库: {dbPath}");
        File.Delete(dbPath);
    }

    builder.Services.AddSingleton<IConfigStore>(sp =>
        new LiteDbConfigStore(sp.GetRequiredService<ILogger<LiteDbConfigStore>>(), dbPath));

    // ============================================================================
    // 注册配置仓储
    // ============================================================================

    builder.Services.AddSingleton<IMainLineOptionsRepository, LiteDbMainLineOptionsRepository>();
    builder.Services.AddSingleton<IInfeedLayoutOptionsRepository, LiteDbInfeedLayoutOptionsRepository>();
    builder.Services.AddSingleton<IChuteConfigRepository, LiteDbChuteConfigRepository>();
    builder.Services.AddSingleton<IUpstreamConnectionOptionsRepository, LiteDbUpstreamConnectionOptionsRepository>();

    // ============================================================================
    // 加载或种子配置
    // ============================================================================

    var host = builder.Build();
    var configStore = host.Services.GetRequiredService<IConfigStore>();

    // 检查并种子配置
    var mainLineRepo = host.Services.GetRequiredService<IMainLineOptionsRepository>();
    var infeedRepo = host.Services.GetRequiredService<IInfeedLayoutOptionsRepository>();
    var chuteRepo = host.Services.GetRequiredService<IChuteConfigRepository>();
    var upstreamRepo = host.Services.GetRequiredService<IUpstreamConnectionOptionsRepository>();

    if (!await configStore.ExistsAsync("MainLineControlOptions"))
    {
        Console.WriteLine("种子主线控制选项...");
        var defaultMainLine = NarrowBeltDefaultConfigSeeder.CreateDefaultMainLineOptions();
        await mainLineRepo.SaveAsync(defaultMainLine);
    }

    if (!await configStore.ExistsAsync("InfeedLayoutOptions"))
    {
        Console.WriteLine("种子入口布局选项...");
        var defaultInfeed = NarrowBeltDefaultConfigSeeder.CreateDefaultInfeedLayoutOptions();
        await infeedRepo.SaveAsync(defaultInfeed);
    }

    if (!await configStore.ExistsAsync("ChuteConfigSet"))
    {
        Console.WriteLine("种子格口配置...");
        var defaultChutes = NarrowBeltDefaultConfigSeeder.CreateDefaultChuteConfigs(10, 10);
        await chuteRepo.SaveAsync(defaultChutes);
    }

    if (!await configStore.ExistsAsync("UpstreamConnectionOptions"))
    {
        Console.WriteLine("种子上游连接选项...");
        var defaultUpstream = NarrowBeltDefaultConfigSeeder.CreateDefaultUpstreamOptions(true);
        await upstreamRepo.SaveAsync(defaultUpstream);
    }

    Console.WriteLine("配置加载完成\n");

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
        ParcelGenerationIntervalSeconds = 0.1, // 快速生成
        SimulationDurationSeconds = 60
    };

    builder.Services.AddSingleton(simulationConfig);

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
                CartOffsetFromOrigin = i * 2,
                MaxOpenDuration = TimeSpan.FromMilliseconds(300)
            });
        }
        return provider;
    });

    // ============================================================================
    // 注册 E2E Runner
    // ============================================================================

    builder.Services.AddSingleton<EndToEndSimulationRunner>();

    // ============================================================================
    // 配置日志
    // ============================================================================

    builder.Logging.ClearProviders();
    builder.Logging.AddConsole();
    builder.Logging.SetMinimumLevel(LogLevel.Information);

    // ============================================================================
    // 运行 E2E 仿真
    // ============================================================================

    var app = builder.Build();
    var runner = app.Services.GetRequiredService<EndToEndSimulationRunner>();

    Console.WriteLine("开始仿真...\n");
    var report = await runner.RunAsync(parcelCount);

    // ============================================================================
    // 输出报告
    // ============================================================================

    Console.WriteLine("\n════════════════════════════════════════");
    Console.WriteLine("  仿真报告");
    Console.WriteLine("════════════════════════════════════════");
    Console.WriteLine($"本次仿真已完成:");
    Console.WriteLine($"- 包裹总数: {report.Statistics.TotalParcels}");
    Console.WriteLine($"- 正常落格: {report.Statistics.SuccessfulSorts}");
    Console.WriteLine($"- 强排: {report.Statistics.ForceEjects}");
    Console.WriteLine($"- 误分: {report.Statistics.Missorts}");
    Console.WriteLine($"- 小车环长度: {report.CartRing.Length}");
    Console.WriteLine($"- 目标速度: {report.MainDrive.TargetSpeedMmps:F1} mm/s");
    Console.WriteLine($"- 平均速度: {report.MainDrive.AverageSpeedMmps:F1} mm/s");
    Console.WriteLine($"- 速度标准差: {report.MainDrive.SpeedStdDevMmps:F2} mm/s");
    Console.WriteLine($"- 仿真耗时: {report.Statistics.DurationSeconds:F2} 秒");
    Console.WriteLine("════════════════════════════════════════\n");

    // ============================================================================
    // 保存 JSON 报告
    // ============================================================================

    if (!string.IsNullOrEmpty(outputPath))
    {
        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(report, jsonOptions);
        await File.WriteAllTextAsync(outputPath, json);
        Console.WriteLine($"报告已保存到: {outputPath}");
    }
}

static async Task RunTraditionalSimulationAsync()
{
    Console.WriteLine("═══ 运行传统仿真模式 ═══\n");

    var builder = Host.CreateApplicationBuilder();

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

    builder.Services.AddSingleton(new SortingPlannerOptions
    {
        CartSpacingMm = simulationConfig.CartSpacingMm
    });

    builder.Services.Configure<SortingExecutionOptions>(options =>
    {
        options.ExecutionPeriod = TimeSpan.FromMilliseconds(100);
        options.PlanningHorizon = TimeSpan.FromSeconds(5);
    });

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
                CartOffsetFromOrigin = i * 2,
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
}

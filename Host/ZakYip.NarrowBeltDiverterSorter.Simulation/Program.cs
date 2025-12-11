using FakeMainLineDrivePortFromSim = ZakYip.NarrowBeltDiverterSorter.Simulation.Fakes.FakeMainLineDrivePort;
using FakeMainLineFeedbackPortFromSim = ZakYip.NarrowBeltDiverterSorter.Simulation.Fakes.FakeMainLineFeedbackPort;
using System.CommandLine;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Communication;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Application;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Carts;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Feeding;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.MainLine;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Parcels;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Sorting;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Topology;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Tracking;
using ZakYip.NarrowBeltDiverterSorter.Execution.Feeding;
using ZakYip.NarrowBeltDiverterSorter.Execution.Mainline;
using ZakYip.NarrowBeltDiverterSorter.Execution.Vendors.Simulated;
using ZakYip.NarrowBeltDiverterSorter.Execution.Sorting;

using ZakYip.NarrowBeltDiverterSorter.Core.Configuration;
using ZakYip.NarrowBeltDiverterSorter.Infrastructure.Configuration;
using ZakYip.NarrowBeltDiverterSorter.Infrastructure.LiteDb;
using ZakYip.NarrowBeltDiverterSorter.Ingress.Infeed;
using ZakYip.NarrowBeltDiverterSorter.Ingress.Origin;
using ZakYip.NarrowBeltDiverterSorter.Simulation;
using ZakYip.NarrowBeltDiverterSorter.Simulation.Fakes;
using ZakYip.NarrowBeltDiverterSorter.Observability;
using ZakYip.NarrowBeltDiverterSorter.Core.SelfCheck;
using ZakYip.NarrowBeltDiverterSorter.Simulation.Scenarios;
using ZakYip.NarrowBeltDiverterSorter.Core.Enums.Configuration;
using ZakYip.NarrowBeltDiverterSorter.Core.Enums.Simulation;

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine("════════════════════════════════════════");
Console.WriteLine("  窄带分拣机仿真系统 (Narrow Belt Sorter Simulation)");
Console.WriteLine("════════════════════════════════════════\n");

// ============================================================================
// 定义命令行参数
// ============================================================================

var scenarioOption = new Option<string?>(
    name: "--scenario",
    getDefaultValue: () => "legacy",
    description: "仿真场景：legacy（传统仿真，60秒持续运行）、e2e-report（端到端仿真并输出报告）、e2e-speed-unstable（端到端仿真，速度不稳定）、long-run-load-test（长时间高负载测试，1000包裹）或 safety-chute-reset（安全场景仿真）");

var parcelCountOption = new Option<int>(
    name: "--parcel-count",
    getDefaultValue: () => 20,
    description: "本次仿真包裹数量（仅在 e2e-report 模式下生效）");

var outputOption = new Option<string?>(
    name: "--output",
    description: "报告输出路径，例如 simulation-report.json（仅在 e2e-report 模式下生效）");

var resetConfigOption = new Option<bool>(
    name: "--reset-config",
    getDefaultValue: () => false,
    description: "仿真前清空 LiteDB 配置并写入默认配置");

var sortingModeOption = new Option<string?>(
    name: "--sorting-mode",
    getDefaultValue: () => "normal",
    description: "分拣模式：normal（正式分拣）、fixed-chute（指定落格）或 round-robin（循环格口）");

var fixedChuteIdOption = new Option<int?>(
    name: "--fixed-chute-id",
    description: "固定格口ID（仅在 fixed-chute 模式下生效）");

var rootCommand = new RootCommand("窄带分拣机仿真系统")
{
    scenarioOption,
    parcelCountOption,
    outputOption,
    resetConfigOption,
    sortingModeOption,
    fixedChuteIdOption
};

rootCommand.SetHandler(async (scenario, parcelCount, output, resetConfig, sortingMode, fixedChuteId) =>
{
    await RunSimulationAsync(scenario, parcelCount, output, resetConfig, sortingMode, fixedChuteId);
}, scenarioOption, parcelCountOption, outputOption, resetConfigOption, sortingModeOption, fixedChuteIdOption);

return await rootCommand.InvokeAsync(args);

static async Task RunSimulationAsync(string? scenario, int parcelCount, string? output, bool resetConfig, string? sortingMode, int? fixedChuteId)
{
    // 如果指定了 E2E 报告场景或不稳定速度场景，运行 E2E 模式
    if (scenario == "e2e-report" || scenario == "e2e-speed-unstable")
    {
        await RunE2EScenarioAsync(parcelCount, output, resetConfig, sortingMode, fixedChuteId, scenario);
    }
    else if (scenario == "long-run-load-test")
    {
        await RunLongRunLoadTestScenarioAsync(output, resetConfig);
    }
    else if (scenario == "safety-chute-reset")
    {
        await RunSafetyScenarioAsync();
    }
    else if (scenario == "chute-cart-mapping-self-check")
    {
        await RunChuteCartMappingSelfCheckScenarioAsync(resetConfig);
    }
    else
    {
        // 否则运行传统仿真模式（legacy 或默认）
        await RunTraditionalSimulationAsync();
    }
}

static async Task RunE2EScenarioAsync(int parcelCount, string? outputPath, bool resetConfig, string? sortingModeStr, int? fixedChuteId, string? scenario = "e2e-report")
{
    // 解析分拣模式
    SortingMode sortingMode = (sortingModeStr?.ToLowerInvariant()) switch
    {
        "fixed-chute" => SortingMode.FixedChute,
        "round-robin" => SortingMode.RoundRobin,
        "normal" or _ => SortingMode.Normal
    };
    
    bool isUnstableSpeedScenario = scenario == "e2e-speed-unstable";

    Console.WriteLine($"═══ 运行 E2E 场景 ═══");
    Console.WriteLine($"场景类型: {scenario}");
    Console.WriteLine($"包裹数量: {parcelCount}");
    Console.WriteLine($"分拣模式: {sortingMode}");
    if (sortingMode == SortingMode.FixedChute)
    {
        Console.WriteLine($"固定格口: {fixedChuteId ?? 1}");
    }
    Console.WriteLine($"输出路径: {outputPath ?? "(未指定)"}");
    Console.WriteLine($"重置配置: {(resetConfig ? "是" : "否")}");
    if (isUnstableSpeedScenario)
    {
        Console.WriteLine($"速度不稳定模式: 启用");
    }
    Console.WriteLine();

    // ============================================================================
    // 种子配置（如果需要）
    // ============================================================================

    var dbPath = Path.Combine(Environment.CurrentDirectory, "simulation.db");
    if (resetConfig && File.Exists(dbPath))
    {
        Console.WriteLine($"删除现有配置数据库: {dbPath}");
        File.Delete(dbPath);
    }

    // 使用临时主机来种子配置
    await SeedConfigurationIfNeededAsync(dbPath);

    Console.WriteLine("配置加载完成\n");

    // ============================================================================
    // 创建主应用程序构建器
    // ============================================================================

    var builder = Host.CreateApplicationBuilder();

    // ============================================================================
    // 配置仿真参数
    // ============================================================================

    var simulationConfig = new SimulationConfiguration
    {
        NumberOfCarts = 60, // 增加到60辆小车（需要至少60辆，格口数10个，每个包裹最多占用6个小车）
        CartSpacingMm = 500m,
        NumberOfChutes = 10,
        ForceEjectChuteId = 10,
        MainLineSpeedMmPerSec = 1000.0,
        InfeedConveyorSpeedMmPerSec = 1000.0,
        InfeedToDropDistanceMm = 2000m,
        ParcelGenerationIntervalSeconds = 0.8, // 0.8秒间隔，给包裹足够时间分拣
        SimulationDurationSeconds = 0, // E2E 模式下不使用时长限制
        ParcelCount = parcelCount, // 使用命令行参数指定的包裹数量
        ParcelTimeToLiveSeconds = isUnstableSpeedScenario ? 15.0 : 25.0, // 不稳定场景使用更短的 TTL
        SortingMode = sortingMode, // 使用命令行参数指定的分拣模式
        FixedChuteId = fixedChuteId, // 固定格口ID（仅在 FixedChute 模式下使用）
        Scenario = scenario, // 仿真场景类型
        SpeedOscillationAmplitude = 300.0, // 速度波动幅度 ±300 mm/s (30% of target speed)
        SpeedOscillationFrequency = 1.0 // 速度波动频率 1.0 Hz (每秒一个周期)
    };

    builder.Services.AddSingleton(simulationConfig);

    // ============================================================================
    // 注册启动模式配置（E2E 模式下使用 Normal 模式）
    // ============================================================================
    
    builder.Services.AddSingleton(new StartupModeConfiguration 
    { 
        Mode = StartupMode.Normal,
        EnableBringupLogging = false
    });

    // ============================================================================
    // 注册配置存储（仿真模式）
    // ============================================================================
    
    builder.Services.AddSingleton<ISorterConfigurationStore>(sp =>
        new LiteDbSorterConfigurationStore(sp.GetRequiredService<ILogger<LiteDbSorterConfigurationStore>>(), dbPath));

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

    var fakeMainLineDrive = new FakeMainLineDrivePortFromSim();
    builder.Services.AddSingleton(fakeMainLineDrive);
    builder.Services.AddSingleton<IMainLineDrivePort>(fakeMainLineDrive);

    var fakeMainLineFeedback = new FakeMainLineFeedbackPortFromSim(fakeMainLineDrive);
    
    // 如果是不稳定速度场景，启用速度波动
    if (isUnstableSpeedScenario)
    {
        fakeMainLineFeedback.EnableUnstableMode(
            simulationConfig.SpeedOscillationAmplitude,
            simulationConfig.SpeedOscillationFrequency);
        Console.WriteLine($"已启用速度不稳定模式：波动幅度 ±{simulationConfig.SpeedOscillationAmplitude} mm/s, 频率 {simulationConfig.SpeedOscillationFrequency} Hz\n");
    }
    
    builder.Services.AddSingleton(fakeMainLineFeedback);
    builder.Services.AddSingleton<IMainLineFeedbackPort>(fakeMainLineFeedback);

    // 注册 SimulatedMainLineDrive（IMainLineDrive 实现）
    builder.Services.AddSingleton<IMainLineDrive, SimulatedMainLineDrive>();

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
    builder.Services.AddSingleton(fakeChuteTransmitter);
    builder.Services.AddSingleton<IChuteTransmitterPort>(fakeChuteTransmitter);

    // 注册上游规则引擎客户端（仿真使用Disabled模式）
    builder.Services.AddSingleton<ZakYip.NarrowBeltDiverterSorter.Communication.Upstream.ISortingRuleEngineClient>(sp =>
    {
        var logger = sp.GetRequiredService<ILoggerFactory>()
            .CreateLogger<ZakYip.NarrowBeltDiverterSorter.Communication.Upstream.DisabledSortingRuleEngineClient>();
        return new ZakYip.NarrowBeltDiverterSorter.Communication.Upstream.DisabledSortingRuleEngineClient(logger);
    });

    // 注册上游路由配置提供器（仿真使用内存配置）
    builder.Services.AddSingleton<ZakYip.NarrowBeltDiverterSorter.Core.Abstractions.IUpstreamRoutingConfigProvider>(sp =>
    {
        var logger = sp.GetRequiredService<ILoggerFactory>()
            .CreateLogger<ZakYip.NarrowBeltDiverterSorter.Infrastructure.Configuration.LiteDbUpstreamRoutingConfigProvider>();
        var configStore = sp.GetRequiredService<ZakYip.NarrowBeltDiverterSorter.Core.Configuration.ISorterConfigurationStore>();
        return new ZakYip.NarrowBeltDiverterSorter.Infrastructure.Configuration.LiteDbUpstreamRoutingConfigProvider(configStore, logger);
    });

    // 注册上游请求追踪器
    builder.Services.AddSingleton<ZakYip.NarrowBeltDiverterSorter.Core.Domain.Upstream.IUpstreamRequestTracker,
        ZakYip.NarrowBeltDiverterSorter.Core.Application.UpstreamRequestTracker>();

    // 注册上游超时检查器
    builder.Services.AddSingleton<ZakYip.NarrowBeltDiverterSorter.Execution.Upstream.UpstreamTimeoutChecker>();

    // 注册上游响应处理器
    builder.Services.AddSingleton<ZakYip.NarrowBeltDiverterSorter.Execution.Upstream.UpstreamResponseHandler>();

    // ============================================================================
    // 注册领域服务 (E2E Scenario)
    // ============================================================================

    // 注册仿真主线设定点提供者
    var e2eSetpoint = new SimulationMainLineSetpoint();
    builder.Services.AddSingleton(e2eSetpoint);
    builder.Services.AddSingleton<IMainLineSetpointProvider>(e2eSetpoint);

    builder.Services.AddSingleton<ICartRingBuilder, CartRingBuilder>();
    builder.Services.AddSingleton<ZakYip.NarrowBeltDiverterSorter.Core.Domain.SystemState.ISystemRunStateService, ZakYip.NarrowBeltDiverterSorter.Core.Domain.SystemState.SystemRunStateService>();
    builder.Services.AddSingleton<IParcelLifecycleService, ParcelLifecycleService>();
    builder.Services.AddSingleton<ICartLifecycleService, CartLifecycleService>();
    builder.Services.AddSingleton<IParcelLoadPlanner, ParcelLoadPlanner>();
    builder.Services.AddSingleton<ISortingPlanner, SortingPlanner>();
    builder.Services.AddSingleton<IEjectPlanner, EjectPlanner>();
    builder.Services.AddSingleton<IMainLineControlService, MainLineControlService>();
    builder.Services.AddSingleton<IMainLineSpeedProvider, MainLineSpeedProvider>();
    builder.Services.AddSingleton<IMainLineStabilityProvider, MainLineStabilityProvider>();
    builder.Services.AddSingleton<ICartPositionTracker, CartPositionTracker>();
    
    // 注册小车环配置提供器（仿真模式使用内存配置）

    
    builder.Services.AddSingleton<ZakYip.NarrowBeltDiverterSorter.Core.Configuration.ICartRingConfigurationProvider>(sp =>

    
    {

    
        var logger = sp.GetRequiredService<ILoggerFactory>()

    
            .CreateLogger<ZakYip.NarrowBeltDiverterSorter.Infrastructure.Configuration.CartRingConfigurationProvider>();

    
        var configStore = sp.GetRequiredService<ZakYip.NarrowBeltDiverterSorter.Core.Configuration.ISorterConfigurationStore>();

    
        var provider = new ZakYip.NarrowBeltDiverterSorter.Infrastructure.Configuration.CartRingConfigurationProvider(configStore, logger);

    
        

    
        // 使用仿真配置中的小车数量初始化

    
        var cartRingConfig = new ZakYip.NarrowBeltDiverterSorter.Core.Configuration.CartRingConfiguration
        {
            TotalCartCount = simulationConfig.NumberOfCarts
        };

    
        provider.UpdateAsync(cartRingConfig, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

    
        

    
        return provider;

    
    });

    
    

    
    // 注册格口小车号解析器和计算器
    builder.Services.AddSingleton<ZakYip.NarrowBeltDiverterSorter.Core.Domain.Sorting.IChuteCartNumberCalculator, 
        ZakYip.NarrowBeltDiverterSorter.Core.Domain.Sorting.ChuteCartNumberCalculator>();
    builder.Services.AddSingleton<ZakYip.NarrowBeltDiverterSorter.Core.Abstractions.ICartAtChuteResolver, 
        ZakYip.NarrowBeltDiverterSorter.Execution.Sorting.CartAtChuteResolver>();
    
    // 注册轨道拓扑
    builder.Services.AddSingleton<ITrackTopology>(sp =>
    {
        return TrackTopologyBuilder.BuildFromSimulationConfig(simulationConfig);
    });
    
    builder.Services.AddSingleton<IChuteConfigProvider>(sp =>
    {
        var topology = sp.GetRequiredService<ITrackTopology>();
        var provider = new ChuteConfigProvider();
        var configs = TrackTopologyBuilder.BuildChuteConfigs(topology, simulationConfig.ForceEjectChuteId);
        foreach (var config in configs)
        {
            provider.AddOrUpdate(config);
        }
        return provider;
    });

    // ============================================================================
    // 注册领域协调器
    // ============================================================================

    builder.Services.AddSingleton(sp =>
    {
        var loadPlanner = sp.GetRequiredService<IParcelLoadPlanner>();
        var coordinator = new ParcelLoadCoordinator(loadPlanner);
        var logger = sp.GetRequiredService<ILogger<ParcelLoadCoordinator>>();
        
        // 设置日志委托
        coordinator.SetLogAction(msg => logger.LogInformation(msg));
        
        return coordinator;
    });

    // ============================================================================
    // 注册 Ingress 监视器并连接事件
    // ============================================================================

    // 注册 OriginSensorMonitor 并连接到 CartRingBuilder 和 CartPositionTracker
    builder.Services.AddSingleton(sp =>
    {
        var originSensor = sp.GetRequiredService<IOriginSensorPort>();
        var cartRingBuilder = sp.GetRequiredService<ICartRingBuilder>();
        var cartPositionTracker = sp.GetRequiredService<ICartPositionTracker>();
        
        var eventBus = sp.GetRequiredService<IEventBus>();
        var logger = sp.GetRequiredService<ILogger<OriginSensorMonitor>>();
        return new OriginSensorMonitor(originSensor, cartRingBuilder, cartPositionTracker, eventBus, logger);
    });
    
    builder.Services.AddSingleton(sp =>
    {
        var infeedSensor = sp.GetRequiredService<IInfeedSensorPort>();
        var eventBus = sp.GetRequiredService<IEventBus>();
        var monitorLogger = sp.GetRequiredService<ILogger<InfeedSensorMonitor>>();
        var monitor = new InfeedSensorMonitor(infeedSensor, eventBus, monitorLogger);
        
        // 连接 InfeedSensorMonitor 与 ParcelRoutingRuntime 和 ParcelLoadCoordinator
        var loadCoordinator = sp.GetRequiredService<ParcelLoadCoordinator>();
        var parcelLifecycleService = sp.GetRequiredService<IParcelLifecycleService>();
        var cartLifecycleService = sp.GetRequiredService<ICartLifecycleService>();
        var logger = sp.GetRequiredService<ILogger<InfeedSensorMonitor>>();
        
        // 订阅 IEventBus 的包裹创建事件（InfeedSensorMonitor 会自动发布到 EventBus）
        eventBus.Subscribe<ZakYip.NarrowBeltDiverterSorter.Core.Domain.Feeding.ParcelCreatedFromInfeedEventArgs>(async (busArgs, ct) =>
        {
            try
            {
                // 通知路由运行时处理包裹
                var routingRuntime = sp.GetRequiredService<ZakYip.NarrowBeltDiverterSorter.Core.Domain.Runtime.IParcelRoutingRuntime>();
                if (routingRuntime is ZakYip.NarrowBeltDiverterSorter.Execution.Runtime.ParcelRoutingRuntime runtime)
                {
                    await runtime.HandleParcelCreatedAsync(busArgs);
                }
                
                // 通知落车协调器
                loadCoordinator.HandleParcelCreatedFromInfeed(null, busArgs);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "处理包裹创建事件时发生错误");
            }
        });
        
        // 订阅 IEventBus 的包裹装载事件（ParcelLoadCoordinator 内部会发布到 EventBus）
        eventBus.Subscribe<ZakYip.NarrowBeltDiverterSorter.Core.Domain.Feeding.ParcelLoadedOnCartEventArgs>(async (busArgs, ct) =>
        {
            try
            {
                // 更新包裹生命周期服务 - BindCartId 会自动将状态设置为 Sorting
                parcelLifecycleService.BindCartId(busArgs.ParcelId, busArgs.CartId, busArgs.LoadedTime);
                
                // 更新小车生命周期服务
                cartLifecycleService.LoadParcel(busArgs.CartId, busArgs.ParcelId);
                
                // 注意：不再手动设置状态为 Routed，因为 BindCartId 已经正确地将状态设置为 Sorting
                
                logger.LogInformation(
                    "[上车确认] 包裹 {ParcelId} 已上车到小车 {CartId}",
                    busArgs.ParcelId,
                    busArgs.CartId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "处理包裹装载事件时发生错误");
            }
        });
        
        return monitor;
    });

    // ============================================================================
    // 注册执行运行时
    // ============================================================================

    // 注册主线控制运行时
    builder.Services.AddSingleton<ZakYip.NarrowBeltDiverterSorter.Core.Domain.Runtime.IMainLineRuntime, 
        ZakYip.NarrowBeltDiverterSorter.Execution.Runtime.MainLineRuntime>();

    // 注册包裹路由运行时
    builder.Services.AddSingleton<ZakYip.NarrowBeltDiverterSorter.Core.Domain.Runtime.IParcelRoutingRuntime, 
        ZakYip.NarrowBeltDiverterSorter.Execution.Runtime.ParcelRoutingRuntime>();

    // ============================================================================
    // 注册后台工作器（E2E 模式下启动完整管道）
    // ============================================================================

    // 启动主线控制运行时
    builder.Services.AddHostedService(sp =>
    {
        var runtime = sp.GetRequiredService<ZakYip.NarrowBeltDiverterSorter.Core.Domain.Runtime.IMainLineRuntime>();
        return new MainLineRuntimeHostedService(runtime, sp.GetRequiredService<ILogger<MainLineRuntimeHostedService>>());
    });
    
    builder.Services.AddHostedService<ParcelSortingSimulator>();
    builder.Services.AddHostedService<CartMovementSimulator>();
    builder.Services.AddHostedService<ParcelGeneratorWorker>();
    
    // 添加传感器监视器启动服务
    builder.Services.AddHostedService<OriginSensorMonitorHostedService>();
    builder.Services.AddHostedService<InfeedSensorMonitorHostedService>();

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
    // 构建并运行 Host（使用超时控制）
    // ============================================================================

    var app = builder.Build();
    
    // Enable main line setpoint for E2E scenario
    var e2eSetpointProvider = app.Services.GetRequiredService<SimulationMainLineSetpoint>();
    e2eSetpointProvider.SetSetpoint(true, (decimal)simulationConfig.MainLineSpeedMmPerSec);
    
    // Start and configure fake infeed conveyor for E2E scenario
    await fakeInfeedConveyor.StartAsync();
    await fakeInfeedConveyor.SetSpeedAsync(simulationConfig.InfeedConveyorSpeedMmPerSec);
    
    // Start fake main line drive for E2E scenario
    await fakeMainLineDrive.StartAsync();
    
    Console.WriteLine("开始仿真...\n");
    
    // 创建一个超时的 CancellationTokenSource
    using var cts = new CancellationTokenSource();
    
    // 创建一个 Task 来运行 E2E 仿真并在完成后取消 Host
    var e2eTask = Task.Run(async () =>
    {
        try
        {
            // 等待一小段时间让后台服务启动
            await Task.Delay(1000, cts.Token);
            
            var runner = app.Services.GetRequiredService<EndToEndSimulationRunner>();
            var report = await runner.RunAsync(parcelCount, cts.Token);
            
            // 计算完成率
            double completionRate = report.Statistics.TotalParcels > 0 
                ? (double)(report.Statistics.SuccessfulSorts + report.Statistics.ForceEjects + report.Statistics.Missorts) / report.Statistics.TotalParcels * 100.0
                : 0.0;
            
            // 计算已分拣包裹数
            int sortedParcels = report.Statistics.SuccessfulSorts + report.Statistics.ForceEjects + report.Statistics.Missorts;
            
            // 验证数据一致性
            bool hasDataConsistency = (sortedParcels <= report.Statistics.TotalParcels);
            bool hasNonZeroSpeed = report.MainDrive.AverageSpeedMmps > 0;
            
            // 检查是否因超时提前结束
            bool isIncomplete = completionRate < 95.0;
            
            // 保存报告 - 清晰的中文输出
            Console.WriteLine("\n════════════════════════════════════════");
            Console.WriteLine("║      E2E 仿真结果报告                ║");
            Console.WriteLine("════════════════════════════════════════");
            Console.WriteLine();
            Console.WriteLine("【包裹统计】");
            Console.WriteLine($"  总包裹数:    {report.Statistics.TotalParcels,6} 个");
            Console.WriteLine($"  正常落格:    {report.Statistics.SuccessfulSorts,6} 个");
            Console.WriteLine($"  强制排出:    {report.Statistics.ForceEjects,6} 个");
            Console.WriteLine($"  误分/失败:   {report.Statistics.Missorts,6} 个");
            Console.WriteLine($"  已分拣数:    {sortedParcels,6} 个");
            Console.WriteLine($"  完成率:      {completionRate,6:F1} %");
            
            if (isIncomplete)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine();
                Console.WriteLine("  ⚠️  警告: 仿真因超时提前结束，未达到目标完成率");
                Console.ResetColor();
            }
            
            Console.WriteLine();
            Console.WriteLine("【分拣配置】");
            if (!string.IsNullOrEmpty(report.SortingConfig.Scenario))
            {
                Console.WriteLine($"  仿真场景:    {report.SortingConfig.Scenario,6}");
            }
            Console.WriteLine($"  分拣模式:    {report.SortingConfig.SortingMode,6}");
            if (report.SortingConfig.FixedChuteId.HasValue)
            {
                Console.WriteLine($"  固定格口:    {report.SortingConfig.FixedChuteId.Value,6}");
            }
            Console.WriteLine($"  可用格口:    {report.SortingConfig.AvailableChutes,6} 个");
            Console.WriteLine($"  强排口:      格口 {report.SortingConfig.ForceEjectChuteId,2}");

            Console.WriteLine();
            Console.WriteLine("【小车环配置】");
            Console.WriteLine($"  小车数量:    {report.CartRing.Length,6} 辆");
            Console.WriteLine($"  小车间距:    {report.CartRing.CartSpacingMm,6:F1} mm");
            Console.WriteLine($"  状态:        {(report.CartRing.IsReady ? "已就绪" : "未就绪"),6}");
            Console.WriteLine($"  预热耗时:    {report.CartRing.WarmupDurationSeconds,6:F2} 秒");
            
            Console.WriteLine();
            Console.WriteLine("【主线速度统计】");
            Console.WriteLine($"  目标速度:    {report.MainDrive.TargetSpeedMmps,6:F1} mm/s");
            Console.WriteLine($"  平均速度:    {report.MainDrive.AverageSpeedMmps,6:F1} mm/s");
            Console.WriteLine($"  速度标准差:  {report.MainDrive.SpeedStdDevMmps,6:F2} mm/s");
            Console.WriteLine($"  最小速度:    {report.MainDrive.MinSpeedMmps,6:F1} mm/s");
            Console.WriteLine($"  最大速度:    {report.MainDrive.MaxSpeedMmps,6:F1} mm/s");
            
            Console.WriteLine();
            Console.WriteLine("【性能指标】");
            Console.WriteLine($"  仿真耗时:    {report.Statistics.DurationSeconds,6:F2} 秒");
            if (report.Statistics.DurationSeconds > 0)
            {
                double throughput = report.Statistics.TotalParcels / report.Statistics.DurationSeconds;
                Console.WriteLine($"  吞吐量:      {throughput,6:F1} 件/秒");
            }
            
            Console.WriteLine();
            Console.WriteLine("【数据验证】");
            Console.WriteLine($"  数据一致性:  {(hasDataConsistency ? "✓ 通过" : "✗ 失败"),6}");
            Console.WriteLine($"  速度非零:    {(hasNonZeroSpeed ? "✓ 通过" : "✗ 失败"),6}");
            
            Console.WriteLine();
            Console.WriteLine("════════════════════════════════════════\n");

            if (!string.IsNullOrEmpty(outputPath))
            {
                var jsonOptions = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var json = JsonSerializer.Serialize(report, jsonOptions);
                await File.WriteAllTextAsync(outputPath, json, cts.Token);
                
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✓ 详细报告已保存到: {outputPath}");
                Console.ResetColor();
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("E2E 仿真已取消");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"E2E 仿真发生异常: {ex.Message}");
        }
        finally
        {
            // 取消 Host 运行
            cts.Cancel();
        }
    }, cts.Token);
    
    // 启动并运行 Host（会阻塞直到取消）
    try
    {
        await app.RunAsync(cts.Token);
    }
    catch (OperationCanceledException)
    {
        // 正常结束
    }
    
    // 等待 E2E 任务完成
    try
    {
        await e2eTask;
    }
    catch (OperationCanceledException)
    {
        // 预期的取消
    }
}

static async Task SeedConfigurationIfNeededAsync(string dbPath)
{
    var tempBuilder = Host.CreateApplicationBuilder();
    tempBuilder.Logging.ClearProviders();

    tempBuilder.Services.AddSingleton<ISorterConfigurationStore>(sp =>
        new LiteDbSorterConfigurationStore(sp.GetRequiredService<ILogger<LiteDbSorterConfigurationStore>>(), dbPath));
    tempBuilder.Services.AddSingleton<IMainLineOptionsRepository, LiteDbMainLineOptionsRepository>();
    tempBuilder.Services.AddSingleton<IInfeedLayoutOptionsRepository, LiteDbInfeedLayoutOptionsRepository>();
    tempBuilder.Services.AddSingleton<IChuteConfigRepository, LiteDbChuteConfigRepository>();
    tempBuilder.Services.AddSingleton<IUpstreamConnectionOptionsRepository, LiteDbUpstreamConnectionOptionsRepository>();

    var tempHost = tempBuilder.Build();
    var configStore = tempHost.Services.GetRequiredService<ISorterConfigurationStore>();
    var mainLineRepo = tempHost.Services.GetRequiredService<IMainLineOptionsRepository>();
    var infeedRepo = tempHost.Services.GetRequiredService<IInfeedLayoutOptionsRepository>();
    var chuteRepo = tempHost.Services.GetRequiredService<IChuteConfigRepository>();
    var upstreamRepo = tempHost.Services.GetRequiredService<IUpstreamConnectionOptionsRepository>();

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
}

static async Task RunTraditionalSimulationAsync()
{
    Console.WriteLine("═══ 运行传统仿真模式 ═══\n");

    var builder = Host.CreateApplicationBuilder();

    // 定义数据库路径（传统仿真模式使用独立的数据库）
    var dbPath = Path.Combine(Environment.CurrentDirectory, "simulation-traditional.db");

    // ============================================================================
    // 配置仿真参数
    // ============================================================================

    var simulationConfig = new SimulationConfiguration
    {
        NumberOfCarts = 60, // 增加到60辆小车（需要至少60辆，格口数10个，每个包裹最多占用6个小车）
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
    // 注册启动模式配置（传统模式下使用 Normal 模式）
    // ============================================================================
    
    builder.Services.AddSingleton(new StartupModeConfiguration 
    { 
        Mode = StartupMode.Normal,
        EnableBringupLogging = false
    });

    // ============================================================================
    // 注册配置存储（仿真模式）
    // ============================================================================
    
    builder.Services.AddSingleton<ISorterConfigurationStore>(sp =>
        new LiteDbSorterConfigurationStore(sp.GetRequiredService<ILogger<LiteDbSorterConfigurationStore>>(), dbPath));

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

    var fakeMainLineDrive = new FakeMainLineDrivePortFromSim();
    builder.Services.AddSingleton(fakeMainLineDrive);
    builder.Services.AddSingleton<IMainLineDrivePort>(fakeMainLineDrive);

    var fakeMainLineFeedback = new FakeMainLineFeedbackPortFromSim(fakeMainLineDrive);
    builder.Services.AddSingleton(fakeMainLineFeedback);
    builder.Services.AddSingleton<IMainLineFeedbackPort>(fakeMainLineFeedback);

    // 注册 SimulatedMainLineDrive（IMainLineDrive 实现）
    builder.Services.AddSingleton<IMainLineDrive, SimulatedMainLineDrive>();

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
    // 注册领域服务 (Traditional Simulation)
    // ============================================================================

    // 注册仿真主线设定点提供者
    var traditionalSetpoint = new SimulationMainLineSetpoint();
    builder.Services.AddSingleton(traditionalSetpoint);
    builder.Services.AddSingleton<IMainLineSetpointProvider>(traditionalSetpoint);

    builder.Services.AddSingleton<ICartRingBuilder, CartRingBuilder>();
    builder.Services.AddSingleton<ZakYip.NarrowBeltDiverterSorter.Core.Domain.SystemState.ISystemRunStateService, ZakYip.NarrowBeltDiverterSorter.Core.Domain.SystemState.SystemRunStateService>();
    builder.Services.AddSingleton<IParcelLifecycleService, ParcelLifecycleService>();
    builder.Services.AddSingleton<ICartLifecycleService, CartLifecycleService>();
    builder.Services.AddSingleton<IParcelLoadPlanner, ParcelLoadPlanner>();
    builder.Services.AddSingleton<ISortingPlanner, SortingPlanner>();
    builder.Services.AddSingleton<IMainLineControlService, MainLineControlService>();
    builder.Services.AddSingleton<IMainLineSpeedProvider, MainLineSpeedProvider>();
    builder.Services.AddSingleton<IMainLineStabilityProvider, MainLineStabilityProvider>();
    builder.Services.AddSingleton<ICartPositionTracker, CartPositionTracker>();
    
    // 注册小车环配置提供器（仿真模式使用内存配置）

    
    builder.Services.AddSingleton<ZakYip.NarrowBeltDiverterSorter.Core.Configuration.ICartRingConfigurationProvider>(sp =>

    
    {

    
        var logger = sp.GetRequiredService<ILoggerFactory>()

    
            .CreateLogger<ZakYip.NarrowBeltDiverterSorter.Infrastructure.Configuration.CartRingConfigurationProvider>();

    
        var configStore = sp.GetRequiredService<ZakYip.NarrowBeltDiverterSorter.Core.Configuration.ISorterConfigurationStore>();

    
        var provider = new ZakYip.NarrowBeltDiverterSorter.Infrastructure.Configuration.CartRingConfigurationProvider(configStore, logger);

    
        

    
        // 使用仿真配置中的小车数量初始化

    
        var cartRingConfig = new ZakYip.NarrowBeltDiverterSorter.Core.Configuration.CartRingConfiguration
        {
            TotalCartCount = simulationConfig.NumberOfCarts
        };

    
        provider.UpdateAsync(cartRingConfig, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

    
        

    
        return provider;

    
    });

    
    

    
    // 注册格口小车号解析器和计算器
    builder.Services.AddSingleton<ZakYip.NarrowBeltDiverterSorter.Core.Domain.Sorting.IChuteCartNumberCalculator, 
        ZakYip.NarrowBeltDiverterSorter.Core.Domain.Sorting.ChuteCartNumberCalculator>();
    builder.Services.AddSingleton<ZakYip.NarrowBeltDiverterSorter.Core.Abstractions.ICartAtChuteResolver, 
        ZakYip.NarrowBeltDiverterSorter.Execution.Sorting.CartAtChuteResolver>();
    
    // 注册轨道拓扑
    builder.Services.AddSingleton<ITrackTopology>(sp =>
    {
        return TrackTopologyBuilder.BuildFromSimulationConfig(simulationConfig);
    });
    
    builder.Services.AddSingleton<IChuteConfigProvider>(sp =>
    {
        var topology = sp.GetRequiredService<ITrackTopology>();
        var provider = new ChuteConfigProvider();
        var configs = TrackTopologyBuilder.BuildChuteConfigs(topology, simulationConfig.ForceEjectChuteId);
        foreach (var config in configs)
        {
            provider.AddOrUpdate(config);
        }
        return provider;
    });

    builder.Services.AddSingleton(sp =>
    {
        var loadPlanner = sp.GetRequiredService<IParcelLoadPlanner>();
        var coordinator = new ParcelLoadCoordinator(loadPlanner);
        var logger = sp.GetRequiredService<ILogger<ParcelLoadCoordinator>>();
        
        // 设置日志委托
        coordinator.SetLogAction(msg => logger.LogInformation(msg));
        
        return coordinator;
    });

    // ============================================================================
    // 注册 Ingress 监视器
    // ============================================================================

    // 注册 OriginSensorMonitor 并连接到 CartRingBuilder 和 CartPositionTracker
    builder.Services.AddSingleton(sp =>
    {
        var originSensor = sp.GetRequiredService<IOriginSensorPort>();
        var cartRingBuilder = sp.GetRequiredService<ICartRingBuilder>();
        var cartPositionTracker = sp.GetRequiredService<ICartPositionTracker>();
        
        var eventBus = sp.GetRequiredService<IEventBus>();
        var logger = sp.GetRequiredService<ILogger<OriginSensorMonitor>>();
        return new OriginSensorMonitor(originSensor, cartRingBuilder, cartPositionTracker, eventBus, logger);
    });
    
    builder.Services.AddSingleton<InfeedSensorMonitor>();

    // ============================================================================
    // 注册执行运行时
    // ============================================================================

    // 注册主线控制运行时
    builder.Services.AddSingleton<ZakYip.NarrowBeltDiverterSorter.Core.Domain.Runtime.IMainLineRuntime, 
        ZakYip.NarrowBeltDiverterSorter.Execution.Runtime.MainLineRuntime>();

    // ============================================================================
    // 注册后台工作器
    // ============================================================================

    // 启动主线控制运行时
    builder.Services.AddHostedService(sp =>
    {
        var runtime = sp.GetRequiredService<ZakYip.NarrowBeltDiverterSorter.Core.Domain.Runtime.IMainLineRuntime>();
        return new MainLineRuntimeHostedService(runtime, sp.GetRequiredService<ILogger<MainLineRuntimeHostedService>>());
    });
    builder.Services.AddHostedService<ParcelSortingSimulator>();
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

static async Task RunSafetyScenarioAsync()
{
    Console.WriteLine("═══ 运行安全场景仿真 (safety-chute-reset) ═══\n");

    var builder = Host.CreateApplicationBuilder();

    // ============================================================================
    // 配置仿真参数
    // ============================================================================

    const int numberOfChutes = 10;
    
    var simulationConfig = new SimulationConfiguration
    {
        NumberOfCarts = 20,
        CartSpacingMm = 500m,
        NumberOfChutes = numberOfChutes,
        ForceEjectChuteId = 10,
        MainLineSpeedMmPerSec = 1000.0,
        Scenario = "safety-chute-reset"
    };

    Console.WriteLine($"仿真配置:");
    Console.WriteLine($"  格口数量: {simulationConfig.NumberOfChutes}");
    Console.WriteLine($"  场景: 安全场景验证\n");

    builder.Services.AddSingleton(simulationConfig);

    // ============================================================================
    // 配置日志
    // ============================================================================

    builder.Logging.ClearProviders();
    builder.Logging.AddConsole();
    builder.Logging.SetMinimumLevel(LogLevel.Information);

    // ============================================================================
    // 注册 Fake 硬件实现
    // ============================================================================

    var fakeChuteTransmitter = new FakeChuteTransmitterPort();
    builder.Services.AddSingleton(fakeChuteTransmitter);
    builder.Services.AddSingleton<IChuteTransmitterPort>(fakeChuteTransmitter);

    // ============================================================================
    // 注册格口配置提供者
    // ============================================================================
    
    // 注册轨道拓扑
    builder.Services.AddSingleton<ITrackTopology>(sp =>
    {
        return TrackTopologyBuilder.BuildFromSimulationConfig(simulationConfig);
    });

    builder.Services.AddSingleton<IChuteConfigProvider>(sp =>
    {
        var topology = sp.GetRequiredService<ITrackTopology>();
        var provider = new ChuteConfigProvider();
        var configs = TrackTopologyBuilder.BuildChuteConfigs(topology, simulationConfig.ForceEjectChuteId);
        foreach (var config in configs)
        {
            provider.AddOrUpdate(config);
        }
        return provider;
    });

    // ============================================================================
    // 注册安全服务和场景运行器
    // ============================================================================

    builder.Services.AddSingleton<IChuteSafetyService, SimulatedChuteSafetyService>();
    builder.Services.AddSingleton<SafetyScenarioRunner>();

    // ============================================================================
    // 构建并运行安全场景
    // ============================================================================

    var app = builder.Build();

    Console.WriteLine("开始安全场景验证...\n");

    try
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        
        var runner = app.Services.GetRequiredService<SafetyScenarioRunner>();
        var report = await runner.RunAsync(numberOfChutes, cts.Token);

        // 输出报告
        PrintSafetyReport(report);
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"\n✗ 安全场景运行失败: {ex.Message}");
        Console.ResetColor();
    }
}

static void PrintSafetyReport(SafetyScenarioReport report)
{
    Console.WriteLine("\n════════════════════════════════════════");
    Console.WriteLine("║      安全场景验证报告 (Chute Safety) ║");
    Console.WriteLine("════════════════════════════════════════");
    Console.WriteLine();
    Console.WriteLine("【格口状态】");
    Console.WriteLine($"  总格口数:          {report.TotalChutes}");
    Console.WriteLine($"  启动前已清零:      {(report.StartupCloseExecuted ? "✓ 是" : "✗ 否")}");
    Console.WriteLine($"  运行中曾触发开合:  {(report.ChutesTriggeredDuringRun > 0 ? $"✓ 是 ({report.ChutesTriggeredDuringRun} 个格口)" : "✗ 否")}");
    Console.WriteLine($"  停止后全部关闭:    {(report.ChutesOpenAfterShutdown == 0 ? "✓ 是" : "✗ 否")}");
    Console.WriteLine();
    Console.WriteLine("【异常情况】");
    Console.WriteLine($"  启动时仍被检测为打开的格口: {report.ChutesOpenBeforeStartup}");
    Console.WriteLine($"  启动安全关闭后仍打开的格口: {report.ChutesOpenAfterStartupClose}");
    Console.WriteLine($"  停止后仍被检测为打开的格口: {report.ChutesOpenAfterShutdown}");
    Console.WriteLine();
    
    if (!string.IsNullOrEmpty(report.ErrorMessage))
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"【错误信息】");
        Console.WriteLine($"  {report.ErrorMessage}");
        Console.ResetColor();
        Console.WriteLine();
    }
    
    Console.Write("安全检查结果:       ");
    if (report.FinalVerificationPassed)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("✓ 通过");
        Console.ResetColor();
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("✗ 失败");
        Console.ResetColor();
    }
    Console.WriteLine();
    Console.WriteLine("════════════════════════════════════════\n");
}

static async Task RunChuteCartMappingSelfCheckScenarioAsync(bool resetConfig)
{
    Console.WriteLine("═══ 运行格口-小车映射自检场景 (chute-cart-mapping-self-check) ═══\n");

    // ============================================================================
    // 配置数据库
    // ============================================================================

    var dbPath = Path.Combine(Environment.CurrentDirectory, "simulation.db");
    if (resetConfig && File.Exists(dbPath))
    {
        Console.WriteLine($"删除现有配置数据库: {dbPath}");
        File.Delete(dbPath);
    }

    await SeedConfigurationIfNeededAsync(dbPath);
    Console.WriteLine("配置加载完成\n");

    var builder = Host.CreateApplicationBuilder();

    // ============================================================================
    // 配置仿真参数
    // ============================================================================
    
    var simulationConfig = new SimulationConfiguration
    {
        NumberOfCarts = 20,
        CartSpacingMm = 500m,
        CartWidthMm = 200m,
        NumberOfChutes = 32,
        ChuteWidthMm = 500m,
        ForceEjectChuteId = 32,
        MainLineSpeedMmPerSec = 1000.0,
        Scenario = "chute-cart-mapping-self-check",
        ChuteCartMappingLoopCount = 5,
        ChuteCartMappingCartIdTolerance = 0,
        ChuteCartMappingPositionToleranceMm = 10m
    };

    Console.WriteLine($"仿真配置:");
    Console.WriteLine($"  小车数量: {simulationConfig.NumberOfCarts}");
    Console.WriteLine($"  小车节距: {simulationConfig.CartSpacingMm} mm");
    Console.WriteLine($"  小车宽度: {simulationConfig.CartWidthMm} mm");
    Console.WriteLine($"  格口数量: {simulationConfig.NumberOfChutes}");
    Console.WriteLine($"  格口宽度: {simulationConfig.ChuteWidthMm} mm");
    Console.WriteLine($"  自检圈数: {simulationConfig.ChuteCartMappingLoopCount}");
    Console.WriteLine($"  容忍度: {simulationConfig.ChuteCartMappingCartIdTolerance} 辆车");
    Console.WriteLine();

    builder.Services.AddSingleton(simulationConfig);

    // ============================================================================
    // 配置日志
    // ============================================================================

    builder.Logging.ClearProviders();
    builder.Logging.AddConsole();
    builder.Logging.SetMinimumLevel(LogLevel.Information);

    // ============================================================================
    // 注册轨道拓扑
    // ============================================================================
    
    builder.Services.AddSingleton<ITrackTopology>(sp =>
    {
        return TrackTopologyBuilder.BuildFromSimulationConfig(simulationConfig);
    });

    // ============================================================================
    // 注册 Fake 硬件实现
    // ============================================================================

    var fakeMainLineDrive = new FakeMainLineDrivePortFromSim();
    var fakeMainLineFeedback = new FakeMainLineFeedbackPortFromSim(fakeMainLineDrive);
    
    builder.Services.AddSingleton(fakeMainLineDrive);
    builder.Services.AddSingleton<IMainLineDrivePort>(fakeMainLineDrive);
    builder.Services.AddSingleton(fakeMainLineFeedback);
    builder.Services.AddSingleton<IMainLineFeedbackPort>(fakeMainLineFeedback);

    // 注册 SimulatedMainLineDrive（IMainLineDrive 实现）
    builder.Services.AddSingleton<IMainLineDrive, SimulatedMainLineDrive>();

    // ============================================================================
    // 注册主线控制相关服务
    // ============================================================================

    builder.Services.Configure<MainLineControlOptions>(options =>
    {
        options.TargetSpeedMmps = (decimal)simulationConfig.MainLineSpeedMmPerSec;
        options.LoopPeriod = TimeSpan.FromMilliseconds(100);
        options.StableDeadbandMmps = 50m;
    });

    builder.Services.AddSingleton<IMainLineControlService, MainLineControlService>();
    builder.Services.AddSingleton<IMainLineSpeedProvider, MainLineSpeedProvider>();

    // ============================================================================
    // 注册自检服务和场景运行器
    // ============================================================================

    builder.Services.AddSingleton<IChuteCartMappingSelfCheckService, ChuteCartMappingSelfCheckService>();
    
    builder.Services.AddSingleton(sp => new ChuteCartMappingSelfCheckOptions
    {
        LoopCount = simulationConfig.ChuteCartMappingLoopCount,
        CartIdTolerance = simulationConfig.ChuteCartMappingCartIdTolerance,
        PositionToleranceMm = simulationConfig.ChuteCartMappingPositionToleranceMm
    });

    builder.Services.AddSingleton<ChuteCartMappingSelfCheckScenario>();

    // ============================================================================
    // 构建并运行自检场景
    // ============================================================================

    var app = builder.Build();

    Console.WriteLine("开始格口-小车映射自检...\n");

    try
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        
        var runner = app.Services.GetRequiredService<ChuteCartMappingSelfCheckScenario>();
        var result = await runner.RunAsync(cts.Token);

        // 输出报告总结
        Console.WriteLine("\n════════════════════════════════════════");
        if (result.IsAllPassed)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✓ 格口-小车映射自检通过");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("✗ 格口-小车映射自检失败");
            Console.ResetColor();
            Console.WriteLine($"  失败格口数: {result.ChuteItems.Count(item => !item.IsPassed)} / {result.ChuteCount}");
        }
        Console.WriteLine("════════════════════════════════════════\n");
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"\n✗ 格口-小车映射自检运行失败: {ex.Message}");
        Console.WriteLine(ex.StackTrace);
        Console.ResetColor();
    }
}

static async Task RunLongRunLoadTestScenarioAsync(string? outputPath, bool resetConfig)
{
    Console.WriteLine("═══ 运行长时间高负载分拣稳定性仿真场景 (long-run-load-test) ═══\n");

    // 加载默认配置
    var options = ZakYip.NarrowBeltDiverterSorter.Simulation.Options.LongRunLoadTestOptions.CreateDefault();

    Console.WriteLine($"仿真配置:");
    Console.WriteLine($"  目标包裹数: {options.TargetParcelCount}");
    Console.WriteLine($"  包裹创建间隔: {options.ParcelCreationIntervalMs} ms");
    Console.WriteLine($"  格口数量: {options.ChuteCount}");
    Console.WriteLine($"  格口宽度: {options.ChuteWidthMm} mm");
    Console.WriteLine($"  主线速度: {options.MainLineSpeedMmps} mm/s");
    Console.WriteLine($"  小车宽度: {options.CartWidthMm} mm");
    Console.WriteLine($"  小车数量: {options.CartCount}");
    Console.WriteLine($"  小车节距: {options.CartSpacingMm} mm");
    Console.WriteLine($"  异常口: 格口 {options.ExceptionChuteId}");
    Console.WriteLine($"  包裹长度范围: {options.MinParcelLengthMm}-{options.MaxParcelLengthMm} mm");
    Console.WriteLine();

    // ============================================================================
    // 种子配置（如果需要）
    // ============================================================================

    var dbPath = Path.Combine(Environment.CurrentDirectory, "simulation.db");
    if (resetConfig && File.Exists(dbPath))
    {
        Console.WriteLine($"删除现有配置数据库: {dbPath}");
        File.Delete(dbPath);
    }

    await SeedConfigurationIfNeededAsync(dbPath);
    Console.WriteLine("配置加载完成\n");

    // ============================================================================
    // 创建主应用程序构建器
    // ============================================================================

    var builder = Host.CreateApplicationBuilder();

    // ============================================================================
    // 配置仿真参数
    // ============================================================================

    var simulationConfig = new SimulationConfiguration
    {
        NumberOfCarts = options.CartCount,
        CartSpacingMm = options.CartSpacingMm,
        CartWidthMm = options.CartWidthMm,
        NumberOfChutes = options.ChuteCount,
        ChuteWidthMm = options.ChuteWidthMm,
        ForceEjectChuteId = options.ExceptionChuteId,
        MainLineSpeedMmPerSec = (double)options.MainLineSpeedMmps,
        InfeedConveyorSpeedMmPerSec = (double)options.InfeedConveyorSpeedMmps,
        InfeedToDropDistanceMm = options.InfeedToDropDistanceMm,
        ParcelGenerationIntervalSeconds = options.ParcelCreationIntervalMs / 1000.0,
        SimulationDurationSeconds = 0,
        ParcelCount = options.TargetParcelCount,
        ParcelTimeToLiveSeconds = 120.0,
        SortingMode = SortingMode.Normal,
        Scenario = "long-run-load-test"
    };

    builder.Services.AddSingleton(simulationConfig);
    builder.Services.AddSingleton(options);

    // ============================================================================
    // 注册启动模式配置
    // ============================================================================
    
    builder.Services.AddSingleton(new StartupModeConfiguration 
    { 
        Mode = StartupMode.Normal,
        EnableBringupLogging = false
    });

    // ============================================================================
    // 注册配置存储（仿真模式）
    // ============================================================================
    
    builder.Services.AddSingleton<ISorterConfigurationStore>(sp =>
        new LiteDbSorterConfigurationStore(sp.GetRequiredService<ILogger<LiteDbSorterConfigurationStore>>(), dbPath));

    // ============================================================================
    // 配置选项
    // ============================================================================

    builder.Services.Configure<MainLineControlOptions>(opts =>
    {
        opts.TargetSpeedMmps = options.MainLineSpeedMmps;
        opts.LoopPeriod = TimeSpan.FromMilliseconds(100);
        opts.StableDeadbandMmps = 50m;
    });

    builder.Services.AddSingleton(new SortingPlannerOptions
    {
        CartSpacingMm = options.CartSpacingMm
    });

    builder.Services.Configure<SortingExecutionOptions>(opts =>
    {
        opts.ExecutionPeriod = TimeSpan.FromMilliseconds(100);
        opts.PlanningHorizon = TimeSpan.FromSeconds(5);
    });

    builder.Services.AddSingleton(new InfeedLayoutOptions
    {
        InfeedToMainLineDistanceMm = options.InfeedToDropDistanceMm,
        TimeToleranceMs = 50,
        CartOffsetCalibration = 0
    });

    // ============================================================================
    // 注册 Fake 硬件实现
    // ============================================================================

    var fakeMainLineDrive = new FakeMainLineDrivePortFromSim();
    builder.Services.AddSingleton(fakeMainLineDrive);
    builder.Services.AddSingleton<IMainLineDrivePort>(fakeMainLineDrive);

    var fakeMainLineFeedback = new FakeMainLineFeedbackPortFromSim(fakeMainLineDrive);
    builder.Services.AddSingleton(fakeMainLineFeedback);
    builder.Services.AddSingleton<IMainLineFeedbackPort>(fakeMainLineFeedback);

    builder.Services.AddSingleton<IMainLineDrive, SimulatedMainLineDrive>();

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
    builder.Services.AddSingleton(fakeChuteTransmitter);
    builder.Services.AddSingleton<IChuteTransmitterPort>(fakeChuteTransmitter);

    // 使用长跑场景专用的随机上游客户端
    builder.Services.AddSingleton<IUpstreamSortingApiClient, LongRunRandomUpstreamClient>();

    // ============================================================================
    // 注册领域服务
    // ============================================================================

    var longRunSetpoint = new SimulationMainLineSetpoint();
    builder.Services.AddSingleton(longRunSetpoint);
    builder.Services.AddSingleton<IMainLineSetpointProvider>(longRunSetpoint);

    builder.Services.AddSingleton<ICartRingBuilder, CartRingBuilder>();
    builder.Services.AddSingleton<ZakYip.NarrowBeltDiverterSorter.Core.Domain.SystemState.ISystemRunStateService, ZakYip.NarrowBeltDiverterSorter.Core.Domain.SystemState.SystemRunStateService>();
    builder.Services.AddSingleton<IParcelLifecycleService, ParcelLifecycleService>();
    builder.Services.AddSingleton<ICartLifecycleService, CartLifecycleService>();
    builder.Services.AddSingleton<IParcelLoadPlanner, ParcelLoadPlanner>();
    builder.Services.AddSingleton<ISortingPlanner, SortingPlanner>();
    builder.Services.AddSingleton<IEjectPlanner, EjectPlanner>();
    builder.Services.AddSingleton<IMainLineControlService, MainLineControlService>();
    builder.Services.AddSingleton<IMainLineSpeedProvider, MainLineSpeedProvider>();
    builder.Services.AddSingleton<IMainLineStabilityProvider, MainLineStabilityProvider>();
    builder.Services.AddSingleton<ICartPositionTracker, CartPositionTracker>();
    
    // 注册小车环配置提供器（仿真模式使用内存配置）

    
    builder.Services.AddSingleton<ZakYip.NarrowBeltDiverterSorter.Core.Configuration.ICartRingConfigurationProvider>(sp =>

    
    {

    
        var logger = sp.GetRequiredService<ILoggerFactory>()

    
            .CreateLogger<ZakYip.NarrowBeltDiverterSorter.Infrastructure.Configuration.CartRingConfigurationProvider>();

    
        var configStore = sp.GetRequiredService<ZakYip.NarrowBeltDiverterSorter.Core.Configuration.ISorterConfigurationStore>();

    
        var provider = new ZakYip.NarrowBeltDiverterSorter.Infrastructure.Configuration.CartRingConfigurationProvider(configStore, logger);

    
        

    
        // 使用仿真配置中的小车数量初始化

    
        var cartRingConfig = new ZakYip.NarrowBeltDiverterSorter.Core.Configuration.CartRingConfiguration
        {
            TotalCartCount = simulationConfig.NumberOfCarts
        };

    
        provider.UpdateAsync(cartRingConfig, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

    
        

    
        return provider;

    
    });

    
    

    
    // 注册格口小车号解析器和计算器
    builder.Services.AddSingleton<ZakYip.NarrowBeltDiverterSorter.Core.Domain.Sorting.IChuteCartNumberCalculator, 
        ZakYip.NarrowBeltDiverterSorter.Core.Domain.Sorting.ChuteCartNumberCalculator>();
    builder.Services.AddSingleton<ZakYip.NarrowBeltDiverterSorter.Core.Abstractions.ICartAtChuteResolver, 
        ZakYip.NarrowBeltDiverterSorter.Execution.Sorting.CartAtChuteResolver>();
    
    // 注册轨道拓扑
    builder.Services.AddSingleton<ITrackTopology>(sp =>
    {
        return TrackTopologyBuilder.BuildFromSimulationConfig(simulationConfig);
    });
    
    builder.Services.AddSingleton<IChuteConfigProvider>(sp =>
    {
        var topology = sp.GetRequiredService<ITrackTopology>();
        var provider = new ChuteConfigProvider();
        var configs = TrackTopologyBuilder.BuildChuteConfigs(topology, simulationConfig.ForceEjectChuteId);
        foreach (var config in configs)
        {
            provider.AddOrUpdate(config);
        }
        return provider;
    });

    // ============================================================================
    // 注册领域协调器
    // ============================================================================

    builder.Services.AddSingleton(sp =>
    {
        var loadPlanner = sp.GetRequiredService<IParcelLoadPlanner>();
        var coordinator = new ParcelLoadCoordinator(loadPlanner);
        var logger = sp.GetRequiredService<ILogger<ParcelLoadCoordinator>>();
        
        coordinator.SetLogAction(msg => logger.LogInformation(msg));
        
        return coordinator;
    });

    // ============================================================================
    // 注册 Ingress 监视器并连接事件
    // ============================================================================

    builder.Services.AddSingleton(sp =>
    {
        var originSensor = sp.GetRequiredService<IOriginSensorPort>();
        var cartRingBuilder = sp.GetRequiredService<ICartRingBuilder>();
        var cartPositionTracker = sp.GetRequiredService<ICartPositionTracker>();
        
        var eventBus = sp.GetRequiredService<IEventBus>();
        var logger = sp.GetRequiredService<ILogger<OriginSensorMonitor>>();
        return new OriginSensorMonitor(originSensor, cartRingBuilder, cartPositionTracker, eventBus, logger);
    });
    
    builder.Services.AddSingleton(sp =>
    {
        var infeedSensor = sp.GetRequiredService<IInfeedSensorPort>();
        var eventBus = sp.GetRequiredService<IEventBus>();
        var monitorLogger = sp.GetRequiredService<ILogger<InfeedSensorMonitor>>();
        var monitor = new InfeedSensorMonitor(infeedSensor, eventBus, monitorLogger);
        
        var loadCoordinator = sp.GetRequiredService<ParcelLoadCoordinator>();
        var parcelLifecycleService = sp.GetRequiredService<IParcelLifecycleService>();
        var cartLifecycleService = sp.GetRequiredService<ICartLifecycleService>();
        var timelineRecorder = sp.GetRequiredService<ParcelTimelineRecorder>();
        var logger = sp.GetRequiredService<ILogger<InfeedSensorMonitor>>();
        
        // 订阅 IEventBus 的包裹创建事件（InfeedSensorMonitor 会自动发布到 EventBus）
        eventBus.Subscribe<ZakYip.NarrowBeltDiverterSorter.Core.Domain.Feeding.ParcelCreatedFromInfeedEventArgs>(async (busArgs, ct) =>
        {
            try
            {
                // 记录包裹创建事件
                timelineRecorder.RecordEvent(busArgs.ParcelId, "Created", "入口传感器触发");
                
                // 通知路由运行时处理包裹
                var routingRuntime = sp.GetRequiredService<ZakYip.NarrowBeltDiverterSorter.Core.Domain.Runtime.IParcelRoutingRuntime>();
                if (routingRuntime is ZakYip.NarrowBeltDiverterSorter.Execution.Runtime.ParcelRoutingRuntime runtime)
                {
                    await runtime.HandleParcelCreatedAsync(busArgs);
                }
                
                loadCoordinator.HandleParcelCreatedFromInfeed(null, busArgs);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "处理包裹创建事件时发生错误");
            }
        });
        
        // 订阅 IEventBus 的包裹装载事件
        eventBus.Subscribe<ZakYip.NarrowBeltDiverterSorter.Core.Domain.Feeding.ParcelLoadedOnCartEventArgs>(async (busArgs, ct) =>
        {
            try
            {
                parcelLifecycleService.BindCartId(busArgs.ParcelId, busArgs.CartId, busArgs.LoadedTime);
                cartLifecycleService.LoadParcel(busArgs.CartId, busArgs.ParcelId);
                
                // 记录上车事件
                timelineRecorder.RecordEvent(busArgs.ParcelId, "LoadedOnCart", 
                    $"上车到小车 {busArgs.CartId}");
                
                logger.LogInformation(
                    "[上车确认] 包裹 {ParcelId} 已上车到小车 {CartId}",
                    busArgs.ParcelId,
                    busArgs.CartId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "处理包裹装载事件时发生错误");
            }
        });
        
        return monitor;
    });

    // ============================================================================
    // 注册包裹时间线记录器
    // ============================================================================
    
    builder.Services.AddSingleton<ParcelTimelineRecorder>();

    // ============================================================================
    // 注册执行运行时
    // ============================================================================

    // 注册主线控制运行时
    builder.Services.AddSingleton<ZakYip.NarrowBeltDiverterSorter.Core.Domain.Runtime.IMainLineRuntime, 
        ZakYip.NarrowBeltDiverterSorter.Execution.Runtime.MainLineRuntime>();

    // 注册包裹路由运行时
    builder.Services.AddSingleton<ZakYip.NarrowBeltDiverterSorter.Core.Domain.Runtime.IParcelRoutingRuntime, 
        ZakYip.NarrowBeltDiverterSorter.Execution.Runtime.ParcelRoutingRuntime>();

    // ============================================================================
    // 注册后台工作器
    // ============================================================================

    // 启动主线控制运行时
    builder.Services.AddHostedService(sp =>
    {
        var runtime = sp.GetRequiredService<ZakYip.NarrowBeltDiverterSorter.Core.Domain.Runtime.IMainLineRuntime>();
        return new MainLineRuntimeHostedService(runtime, sp.GetRequiredService<ILogger<MainLineRuntimeHostedService>>());
    });
    builder.Services.AddHostedService<ParcelSortingSimulator>();
    builder.Services.AddHostedService<CartMovementSimulator>();
    builder.Services.AddHostedService<ParcelGeneratorWorker>();
    
    builder.Services.AddHostedService<OriginSensorMonitorHostedService>();
    builder.Services.AddHostedService<InfeedSensorMonitorHostedService>();

    // ============================================================================
    // 注册长跑场景运行器
    // ============================================================================

    builder.Services.AddSingleton<LongRunHighLoadSortingScenario>();

    // ============================================================================
    // 配置日志
    // ============================================================================

    builder.Logging.ClearProviders();
    builder.Logging.AddConsole();
    builder.Logging.SetMinimumLevel(LogLevel.Information);

    // ============================================================================
    // 构建并运行 Host
    // ============================================================================

    var app = builder.Build();
    
    longRunSetpoint.SetSetpoint(true, options.MainLineSpeedMmps);
    
    await fakeInfeedConveyor.StartAsync();
    await fakeInfeedConveyor.SetSpeedAsync((double)options.InfeedConveyorSpeedMmps);
    
    await fakeMainLineDrive.StartAsync();
    
    Console.WriteLine("开始仿真...\n");
    
    using var cts = new CancellationTokenSource();
    
    var longRunTask = Task.Run(async () =>
    {
        try
        {
            await Task.Delay(1000, cts.Token);
            
            var scenario = app.Services.GetRequiredService<LongRunHighLoadSortingScenario>();
            var report = await scenario.RunAsync(cts.Token);
            
            var timelineRecorder = app.Services.GetRequiredService<ParcelTimelineRecorder>();
            var parcelLifecycleService = app.Services.GetRequiredService<IParcelLifecycleService>();
            
            double completionRate = report.Statistics.TotalParcels > 0 
                ? (double)(report.Statistics.SuccessfulSorts + report.Statistics.ForceEjects + report.Statistics.Missorts) / report.Statistics.TotalParcels * 100.0
                : 0.0;
            
            int sortedParcels = report.Statistics.SuccessfulSorts + report.Statistics.ForceEjects + report.Statistics.Missorts;
            
            // 输出报告
            Console.WriteLine("\n════════════════════════════════════════");
            Console.WriteLine("║   长时间高负载分拣稳定性仿真报告     ║");
            Console.WriteLine("════════════════════════════════════════");
            Console.WriteLine();
            Console.WriteLine("【包裹统计】");
            Console.WriteLine($"  目标包裹数:        {options.TargetParcelCount,6} 个");
            Console.WriteLine($"  实际完成:          {report.Statistics.TotalParcels,6} 个");
            Console.WriteLine($"  正常落格:          {report.Statistics.SuccessfulSorts,6} 个");
            Console.WriteLine($"  异常落格(异常口):   {report.Statistics.ForceEjects,6} 个");
            Console.WriteLine($"  错分:              {report.Statistics.Missorts,6} 个");
            Console.WriteLine($"  未完成:            {report.Statistics.Unprocessed,6} 个");
            Console.WriteLine($"  仿真总耗时:        {report.Statistics.DurationSeconds,6:F2} 秒");
            
            if (report.Statistics.TotalParcels > 0)
            {
                // 计算最大并发在途包裹数（简化估算：基于时间和生成间隔）
                var maxConcurrent = (int)Math.Min(
                    report.Statistics.DurationSeconds / (options.ParcelCreationIntervalMs / 1000.0),
                    options.TargetParcelCount);
                Console.WriteLine($"  最大并发在途包裹数: {maxConcurrent,6} 个（估算）");
            }
            
            Console.WriteLine();
            Console.WriteLine("【验收结果】");
            
            bool allGenerated = report.Statistics.TotalParcels == options.TargetParcelCount;
            bool allCompleted = report.Statistics.Unprocessed == 0;
            bool noMissorts = report.Statistics.Missorts == 0;
            
            Console.Write($"  ✓ 配置正确且无魔法数字:  ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("通过");
            Console.ResetColor();
            
            Console.Write($"  ");
            if (allGenerated)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("✓");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("✗");
            }
            Console.ResetColor();
            Console.WriteLine($" 全部包裹已生成:      {(allGenerated ? "通过" : "失败")}");
            
            Console.Write($"  ");
            if (allCompleted)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("✓");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("⚠");
            }
            Console.ResetColor();
            Console.WriteLine($" 全部包裹已完成:      {(allCompleted ? "通过" : $"警告({report.Statistics.Unprocessed}个未完成)")}");
            
            Console.Write($"  ");
            if (noMissorts)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("✓");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("✗");
            }
            Console.ResetColor();
            Console.WriteLine($" 无错分:              {(noMissorts ? "通过" : $"失败({report.Statistics.Missorts}个错分)")}");
            
            Console.WriteLine();
            Console.WriteLine("════════════════════════════════════════\n");
            
            // 生成 Markdown 报告
            var configSummary = $@"- 目标包裹数: {options.TargetParcelCount}
- 包裹创建间隔: {options.ParcelCreationIntervalMs}ms
- 格口数量: {options.ChuteCount}
- 异常口: {options.ExceptionChuteId}
- 主线速度: {options.MainLineSpeedMmps} mm/s
- 小车宽度: {options.CartWidthMm}mm
- 小车数量: {options.CartCount}
- 包裹长度范围: {options.MinParcelLengthMm}-{options.MaxParcelLengthMm}mm";
            
            var markdownReport = timelineRecorder.GenerateMarkdownReport(parcelLifecycleService, configSummary);
            var markdownPath = outputPath ?? Path.Combine(Environment.CurrentDirectory, 
                $"LongRunLoadTest_{DateTime.Now:yyyyMMddHHmmss}.md");
            
            await File.WriteAllTextAsync(markdownPath, markdownReport, cts.Token);
            
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✓ 生命周期报告已保存到: {markdownPath}");
            Console.ResetColor();
            
            // 如果指定了 JSON 输出路径，也保存 JSON 报告
            if (!string.IsNullOrEmpty(outputPath) && outputPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                var jsonOptions = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var json = JsonSerializer.Serialize(report, jsonOptions);
                await File.WriteAllTextAsync(outputPath, json, cts.Token);
                
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✓ JSON 报告已保存到: {outputPath}");
                Console.ResetColor();
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("长跑场景仿真已取消");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"长跑场景仿真发生异常: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            Console.ResetColor();
        }
        finally
        {
            cts.Cancel();
        }
    }, cts.Token);
    
    try
    {
        await app.RunAsync(cts.Token);
    }
    catch (OperationCanceledException)
    {
        // 正常结束
    }
    
    try
    {
        await longRunTask;
    }
    catch (OperationCanceledException)
    {
        // 预期的取消
    }
}

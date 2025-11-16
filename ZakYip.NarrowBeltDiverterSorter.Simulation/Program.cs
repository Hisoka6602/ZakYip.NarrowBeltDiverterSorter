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
    getDefaultValue: () => "legacy",
    description: "仿真场景：legacy（传统仿真，60秒持续运行）或 e2e-report（端到端仿真并输出报告）");

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
    // 如果指定了 E2E 报告场景，运行 E2E 模式
    if (scenario == "e2e-report")
    {
        await RunE2EScenarioAsync(parcelCount, output, resetConfig);
    }
    else
    {
        // 否则运行传统仿真模式（legacy 或默认）
        await RunTraditionalSimulationAsync();
    }
}

static async Task RunE2EScenarioAsync(int parcelCount, string? outputPath, bool resetConfig)
{
    Console.WriteLine($"═══ 运行 E2E 场景 ═══");
    Console.WriteLine($"包裹数量: {parcelCount}");
    Console.WriteLine($"输出路径: {outputPath ?? "(未指定)"}");
    Console.WriteLine($"重置配置: {(resetConfig ? "是" : "否")}\n");

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
        NumberOfCarts = 20,
        CartSpacingMm = 500m,
        NumberOfChutes = 10,
        ForceEjectChuteId = 10,
        MainLineSpeedMmPerSec = 1000.0,
        InfeedConveyorSpeedMmPerSec = 1000.0,
        InfeedToDropDistanceMm = 2000m,
        ParcelGenerationIntervalSeconds = 0.1, // 快速生成
        SimulationDurationSeconds = 0, // E2E 模式下不使用时长限制
        ParcelCount = parcelCount // 使用命令行参数指定的包裹数量
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
    builder.Services.AddSingleton(fakeChuteTransmitter);
    builder.Services.AddSingleton<IChuteTransmitterPort>(fakeChuteTransmitter);

    builder.Services.AddSingleton<IUpstreamSortingApiClient, FakeUpstreamSortingApiClient>();

    // ============================================================================
    // 注册领域服务 (E2E Scenario)
    // ============================================================================

    // 注册仿真主线设定点提供者
    var e2eSetpoint = new SimulationMainLineSetpoint();
    builder.Services.AddSingleton(e2eSetpoint);
    builder.Services.AddSingleton<IMainLineSetpointProvider>(e2eSetpoint);

    builder.Services.AddSingleton<ICartRingBuilder, CartRingBuilder>();
    builder.Services.AddSingleton<IParcelLifecycleService, ParcelLifecycleService>();
    builder.Services.AddSingleton<ICartLifecycleService, CartLifecycleService>();
    builder.Services.AddSingleton<IParcelLoadPlanner, ParcelLoadPlanner>();
    builder.Services.AddSingleton<ISortingPlanner, SortingPlanner>();
    builder.Services.AddSingleton<IEjectPlanner, EjectPlanner>();
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
        
        return new OriginSensorMonitor(originSensor, cartRingBuilder, cartPositionTracker);
    });
    
    builder.Services.AddSingleton(sp =>
    {
        var infeedSensor = sp.GetRequiredService<IInfeedSensorPort>();
        var monitor = new InfeedSensorMonitor(infeedSensor);
        
        // 连接 InfeedSensorMonitor 与 ParcelRoutingWorker 和 ParcelLoadCoordinator
        var routingWorker = sp.GetRequiredService<ParcelRoutingWorker>();
        var loadCoordinator = sp.GetRequiredService<ParcelLoadCoordinator>();
        var parcelLifecycleService = sp.GetRequiredService<IParcelLifecycleService>();
        var cartLifecycleService = sp.GetRequiredService<ICartLifecycleService>();
        var logger = sp.GetRequiredService<ILogger<InfeedSensorMonitor>>();
        
        monitor.ParcelCreatedFromInfeed += async (sender, args) =>
        {
            try
            {
                // 通知路由工作器处理包裹
                await routingWorker.HandleParcelCreatedAsync(args);
                
                // 通知落车协调器
                loadCoordinator.HandleParcelCreatedFromInfeed(sender, args);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "处理包裹创建事件时发生错误");
            }
        };
        
        // 连接 ParcelLoadCoordinator 的装载事件
        loadCoordinator.ParcelLoadedOnCart += (sender, args) =>
        {
            try
            {
                // 更新包裹生命周期服务
                parcelLifecycleService.BindCartId(args.ParcelId, args.CartId, args.LoadedTime);
                
                // 更新小车生命周期服务
                cartLifecycleService.LoadParcel(args.CartId, args.ParcelId);
                
                // 更新路由状态为已路由（已装载到小车）
                parcelLifecycleService.UpdateRouteState(args.ParcelId, ParcelRouteState.Routed);
                
                logger.LogInformation(
                    "包裹 {ParcelId} 已装载到小车 {CartId}",
                    args.ParcelId.Value,
                    args.CartId.Value);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "处理包裹装载事件时发生错误");
            }
        };
        
        return monitor;
    });

    // ============================================================================
    // 注册后台工作器（E2E 模式下启动完整管道）
    // ============================================================================

    builder.Services.AddSingleton<ParcelRoutingWorker>();
    
    builder.Services.AddHostedService<MainLineControlWorker>();
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

    tempBuilder.Services.AddSingleton<IConfigStore>(sp =>
        new LiteDbConfigStore(sp.GetRequiredService<ILogger<LiteDbConfigStore>>(), dbPath));
    tempBuilder.Services.AddSingleton<IMainLineOptionsRepository, LiteDbMainLineOptionsRepository>();
    tempBuilder.Services.AddSingleton<IInfeedLayoutOptionsRepository, LiteDbInfeedLayoutOptionsRepository>();
    tempBuilder.Services.AddSingleton<IChuteConfigRepository, LiteDbChuteConfigRepository>();
    tempBuilder.Services.AddSingleton<IUpstreamConnectionOptionsRepository, LiteDbUpstreamConnectionOptionsRepository>();

    var tempHost = tempBuilder.Build();
    var configStore = tempHost.Services.GetRequiredService<IConfigStore>();
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
    // 注册启动模式配置（传统模式下使用 Normal 模式）
    // ============================================================================
    
    builder.Services.AddSingleton(new StartupModeConfiguration 
    { 
        Mode = StartupMode.Normal,
        EnableBringupLogging = false
    });

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
    // 注册领域服务 (Traditional Simulation)
    // ============================================================================

    // 注册仿真主线设定点提供者
    var traditionalSetpoint = new SimulationMainLineSetpoint();
    builder.Services.AddSingleton(traditionalSetpoint);
    builder.Services.AddSingleton<IMainLineSetpointProvider>(traditionalSetpoint);

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
        
        return new OriginSensorMonitor(originSensor, cartRingBuilder, cartPositionTracker);
    });
    
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

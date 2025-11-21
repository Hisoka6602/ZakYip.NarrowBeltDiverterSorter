using Xunit;
using Xunit.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Communication;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Application;
using ZakYip.NarrowBeltDiverterSorter.Core.Configuration;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Carts;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Feeding;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.MainLine;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Parcels;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Sorting;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Tracking;
using ZakYip.NarrowBeltDiverterSorter.Execution.Feeding;
using ZakYip.NarrowBeltDiverterSorter.Execution.Mainline;
using ZakYip.NarrowBeltDiverterSorter.Execution.Vendors.Simulated;
using ZakYip.NarrowBeltDiverterSorter.Execution.Sorting;
using ZakYip.NarrowBeltDiverterSorter.Host;
using ZakYip.NarrowBeltDiverterSorter.Ingress.Infeed;
using ZakYip.NarrowBeltDiverterSorter.Ingress.Origin;
using ZakYip.NarrowBeltDiverterSorter.Simulation;
using ZakYip.NarrowBeltDiverterSorter.Simulation.Fakes;
using ZakYip.NarrowBeltDiverterSorter.Observability;
using FakeMainLineDrivePortFromSim = ZakYip.NarrowBeltDiverterSorter.Simulation.Fakes.FakeMainLineDrivePort;
using FakeMainLineFeedbackPortFromSim = ZakYip.NarrowBeltDiverterSorter.Simulation.Fakes.FakeMainLineFeedbackPort;

#pragma warning disable CS0618 // E2E 测试需要测试已过时的事件以确保系统完整性。新代码应使用 IEventBus 订阅事件。

namespace ZakYip.NarrowBeltDiverterSorter.E2ETests;

/// <summary>
/// 测试仿真程序的输出结果，确保所有输出结果正常
/// Tests simulation program output to ensure all results are normal
/// </summary>
public class SimulationOutputTests
{
    private readonly ITestOutputHelper _output;

    public SimulationOutputTests(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// 测试 Normal 模式 E2E 仿真输出
    /// </summary>
    [Fact]
    public async Task NormalMode_E2E_Simulation_ShouldProduceValidOutput()
    {
        // Arrange
        const int parcelCount = 10;
        var simulationConfig = new SimulationConfiguration
        {
            NumberOfCarts = 60,
            CartSpacingMm = 500m,
            NumberOfChutes = 10,
            ForceEjectChuteId = 10,
            MainLineSpeedMmPerSec = 1000.0,
            InfeedConveyorSpeedMmPerSec = 1000.0,
            InfeedToDropDistanceMm = 2000m,
            ParcelGenerationIntervalSeconds = 0.8,
            SimulationDurationSeconds = 0,
            ParcelCount = parcelCount,
            ParcelTimeToLiveSeconds = 25.0,
            SortingMode = SortingMode.Normal,
            Scenario = "e2e-report"
        };

        // Act
        var report = await RunE2ESimulationAsync(simulationConfig);

        // Assert - Verify output results are normal
        _output.WriteLine($"Simulation completed: {report.Statistics.TotalParcels} parcels processed");
        
        // 1. 验证包裹统计数据完整性
        Assert.Equal(parcelCount, report.Statistics.TotalParcels);
        Assert.True(report.Statistics.SuccessfulSorts >= 0);
        Assert.True(report.Statistics.ForceEjects >= 0);
        Assert.True(report.Statistics.Missorts >= 0);
        
        // 2. 验证所有包裹都已完成分拣（总数 = 成功 + 强排 + 失败）
        var totalProcessed = report.Statistics.SuccessfulSorts + 
                           report.Statistics.ForceEjects + 
                           report.Statistics.Missorts;
        Assert.Equal(report.Statistics.TotalParcels, totalProcessed);
        
        // 3. 验证分拣配置正确
        Assert.Equal("Normal", report.SortingConfig.SortingMode);
        Assert.Equal(9, report.SortingConfig.AvailableChutes);
        Assert.Equal(10, report.SortingConfig.ForceEjectChuteId);
        
        // 4. 验证小车环配置
        Assert.Equal(60, report.CartRing.Length);
        Assert.Equal(500.0m, report.CartRing.CartSpacingMm);
        Assert.True(report.CartRing.IsReady);
        Assert.True(report.CartRing.WarmupDurationSeconds > 0);
        
        // 5. 验证主线速度统计正常
        Assert.Equal(1000.0m, report.MainDrive.TargetSpeedMmps);
        Assert.True(report.MainDrive.AverageSpeedMmps > 0);
        Assert.True(report.MainDrive.MinSpeedMmps >= 0);
        Assert.True(report.MainDrive.MaxSpeedMmps > 0);
        Assert.True(report.MainDrive.MaxSpeedMmps >= report.MainDrive.MinSpeedMmps);
        
        // 6. 验证性能指标
        Assert.True(report.Statistics.DurationSeconds > 0);
        
        _output.WriteLine("✓ Normal mode E2E simulation output is valid");
    }

    /// <summary>
    /// 测试 FixedChute 模式 E2E 仿真输出
    /// </summary>
    [Fact]
    public async Task FixedChuteMode_E2E_Simulation_ShouldProduceValidOutput()
    {
        // Arrange
        const int parcelCount = 10;
        const int fixedChuteId = 3;
        var simulationConfig = new SimulationConfiguration
        {
            NumberOfCarts = 60,
            CartSpacingMm = 500m,
            NumberOfChutes = 10,
            ForceEjectChuteId = 10,
            MainLineSpeedMmPerSec = 1000.0,
            InfeedConveyorSpeedMmPerSec = 1000.0,
            InfeedToDropDistanceMm = 2000m,
            ParcelGenerationIntervalSeconds = 0.8,
            SimulationDurationSeconds = 0,
            ParcelCount = parcelCount,
            ParcelTimeToLiveSeconds = 25.0,
            SortingMode = SortingMode.FixedChute,
            FixedChuteId = fixedChuteId,
            Scenario = "e2e-report"
        };

        // Act
        var report = await RunE2ESimulationAsync(simulationConfig);

        // Assert
        _output.WriteLine($"FixedChute mode: {report.Statistics.TotalParcels} parcels processed");
        
        // 1. 验证分拣模式和固定格口配置
        Assert.Equal("FixedChute", report.SortingConfig.SortingMode);
        Assert.Equal(fixedChuteId, report.SortingConfig.FixedChuteId);
        
        // 2. 验证成功分拣到固定格口的包裹（排除超时强排的包裹）
        var successfulParcels = report.ParcelDetails?
            .Where(p => p.IsSuccess && !p.IsForceEject)
            .ToList() ?? new List<ParcelDetail>();
        
        if (successfulParcels.Any())
        {
            Assert.All(successfulParcels, p => Assert.Equal(fixedChuteId, p.TargetChuteId));
            Assert.All(successfulParcels, p => Assert.Equal(fixedChuteId, p.ActualChuteId));
        }
        
        // 3. 验证基本输出结果
        Assert.Equal(parcelCount, report.Statistics.TotalParcels);
        var totalProcessed = report.Statistics.SuccessfulSorts + 
                           report.Statistics.ForceEjects + 
                           report.Statistics.Missorts;
        Assert.Equal(report.Statistics.TotalParcels, totalProcessed);
        
        _output.WriteLine($"✓ FixedChute mode E2E simulation output is valid");
    }

    /// <summary>
    /// 测试 RoundRobin 模式 E2E 仿真输出
    /// </summary>
    [Fact]
    public async Task RoundRobinMode_E2E_Simulation_ShouldProduceValidOutput()
    {
        // Arrange
        const int parcelCount = 10;
        var simulationConfig = new SimulationConfiguration
        {
            NumberOfCarts = 60,
            CartSpacingMm = 500m,
            NumberOfChutes = 10,
            ForceEjectChuteId = 10,
            MainLineSpeedMmPerSec = 1000.0,
            InfeedConveyorSpeedMmPerSec = 1000.0,
            InfeedToDropDistanceMm = 2000m,
            ParcelGenerationIntervalSeconds = 0.8,
            SimulationDurationSeconds = 0,
            ParcelCount = parcelCount,
            ParcelTimeToLiveSeconds = 25.0,
            SortingMode = SortingMode.RoundRobin,
            Scenario = "e2e-report"
        };

        // Act
        var report = await RunE2ESimulationAsync(simulationConfig);

        // Assert
        _output.WriteLine($"RoundRobin mode: {report.Statistics.TotalParcels} parcels processed");
        
        // 1. 验证分拣模式
        Assert.Equal("RoundRobin", report.SortingConfig.SortingMode);
        
        // 2. 验证成功分拣的包裹没有分配到强排口
        var successfulParcels = report.ParcelDetails?
            .Where(p => p.IsSuccess && !p.IsForceEject)
            .ToList() ?? new List<ParcelDetail>();
        
        if (successfulParcels.Any())
        {
            Assert.All(successfulParcels, p => Assert.NotEqual(10, p.TargetChuteId));
        }
        
        // 3. 验证基本输出结果
        Assert.Equal(parcelCount, report.Statistics.TotalParcels);
        var totalProcessed = report.Statistics.SuccessfulSorts + 
                           report.Statistics.ForceEjects + 
                           report.Statistics.Missorts;
        Assert.Equal(report.Statistics.TotalParcels, totalProcessed);
        
        _output.WriteLine("✓ RoundRobin mode E2E simulation output is valid");
    }

    /// <summary>
    /// 测试速度不稳定场景的 E2E 仿真输出
    /// </summary>
    [Fact]
    public async Task UnstableSpeedScenario_E2E_Simulation_ShouldProduceValidOutput()
    {
        // Arrange
        const int parcelCount = 10;
        var simulationConfig = new SimulationConfiguration
        {
            NumberOfCarts = 60,
            CartSpacingMm = 500m,
            NumberOfChutes = 10,
            ForceEjectChuteId = 10,
            MainLineSpeedMmPerSec = 1000.0,
            InfeedConveyorSpeedMmPerSec = 1000.0,
            InfeedToDropDistanceMm = 2000m,
            ParcelGenerationIntervalSeconds = 0.8,
            SimulationDurationSeconds = 0,
            ParcelCount = parcelCount,
            ParcelTimeToLiveSeconds = 15.0, // 更短的 TTL
            SortingMode = SortingMode.Normal,
            Scenario = "e2e-speed-unstable",
            SpeedOscillationAmplitude = 300.0, // ±300 mm/s
            SpeedOscillationFrequency = 1.0 // 1 Hz
        };

        // Act
        var report = await RunE2ESimulationAsync(simulationConfig);

        // Assert
        _output.WriteLine($"Unstable speed scenario: {report.Statistics.TotalParcels} parcels processed");
        
        // 1. 验证速度统计显示不稳定性
        var speedRange = report.MainDrive.MaxSpeedMmps - report.MainDrive.MinSpeedMmps;
        Assert.True(speedRange > 100, $"Speed range should show instability, but got {speedRange} mm/s");
        
        // 2. 验证速度标准差较大（表示不稳定）
        Assert.True(report.MainDrive.SpeedStdDevMmps > 10, 
            $"Speed standard deviation should be significant, but got {report.MainDrive.SpeedStdDevMmps} mm/s");
        
        // 3. 验证基本输出完整性
        Assert.Equal(parcelCount, report.Statistics.TotalParcels);
        var totalProcessed = report.Statistics.SuccessfulSorts + 
                           report.Statistics.ForceEjects + 
                           report.Statistics.Missorts;
        Assert.Equal(report.Statistics.TotalParcels, totalProcessed);
        
        _output.WriteLine("✓ Unstable speed scenario E2E simulation output is valid");
    }

    /// <summary>
    /// 运行 E2E 仿真并返回报告
    /// </summary>
    private async Task<SimulationReport> RunE2ESimulationAsync(SimulationConfiguration simulationConfig)
    {
        var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder();

        // 注册配置
        builder.Services.AddSingleton(simulationConfig);
        builder.Services.AddSingleton(new StartupModeConfiguration 
        { 
            Mode = StartupMode.Normal,
            EnableBringupLogging = false
        });

        // 配置选项
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

        // 注册 Fake 硬件
        var fakeMainLineDrive = new FakeMainLineDrivePortFromSim();
        builder.Services.AddSingleton(fakeMainLineDrive);
        builder.Services.AddSingleton<IMainLineDrivePort>(fakeMainLineDrive);

        var fakeMainLineFeedback = new FakeMainLineFeedbackPortFromSim(fakeMainLineDrive);
        
        // 如果是不稳定速度场景，启用速度波动
        if (simulationConfig.Scenario == "e2e-speed-unstable")
        {
            fakeMainLineFeedback.EnableUnstableMode(
                simulationConfig.SpeedOscillationAmplitude,
                simulationConfig.SpeedOscillationFrequency);
        }
        
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

        builder.Services.AddSingleton<IUpstreamSortingApiClient, FakeUpstreamSortingApiClient>();

        // 注册领域服务
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
        
        // 注册轨道拓扑
        builder.Services.AddSingleton<ZakYip.NarrowBeltDiverterSorter.Core.Domain.Topology.ITrackTopology>(sp =>
        {
            return TrackTopologyBuilder.BuildFromSimulationConfig(simulationConfig);
        });
        
        builder.Services.AddSingleton<IChuteConfigProvider>(sp =>
        {
            var topology = sp.GetRequiredService<ZakYip.NarrowBeltDiverterSorter.Core.Domain.Topology.ITrackTopology>();
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
            return coordinator;
        });

        // 注册监视器
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
            
            var routingWorker = sp.GetRequiredService<ParcelRoutingWorker>();
            var loadCoordinator = sp.GetRequiredService<ParcelLoadCoordinator>();
            var parcelLifecycleService = sp.GetRequiredService<IParcelLifecycleService>();
            var cartLifecycleService = sp.GetRequiredService<ICartLifecycleService>();
            
            // 订阅 IEventBus 的包裹创建事件（InfeedSensorMonitor 会自动发布到 EventBus）
            eventBus.Subscribe<ZakYip.NarrowBeltDiverterSorter.Observability.Events.ParcelCreatedFromInfeedEventArgs>(async (busArgs, ct) =>
            {
                var coreArgs = new ZakYip.NarrowBeltDiverterSorter.Core.Domain.Feeding.ParcelCreatedFromInfeedEventArgs
                {
                    ParcelId = new ZakYip.NarrowBeltDiverterSorter.Core.Domain.ParcelId(busArgs.ParcelId),
                    Barcode = busArgs.Barcode,
                    InfeedTriggerTime = busArgs.InfeedTriggerTime
                };
                await routingWorker.HandleParcelCreatedAsync(coreArgs);
                loadCoordinator.HandleParcelCreatedFromInfeed(null, coreArgs);
            });
            
            // 订阅 IEventBus 的包裹装载事件
            eventBus.Subscribe<ZakYip.NarrowBeltDiverterSorter.Observability.Events.ParcelLoadedOnCartEventArgs>(async (busArgs, ct) =>
            {
                var parcelId = new ZakYip.NarrowBeltDiverterSorter.Core.Domain.ParcelId(busArgs.ParcelId);
                var cartId = new ZakYip.NarrowBeltDiverterSorter.Core.Domain.CartId(busArgs.CartId);
                parcelLifecycleService.BindCartId(parcelId, cartId, busArgs.LoadedAt);
                cartLifecycleService.LoadParcel(cartId, parcelId);
            });
            
            return monitor;
        });

        // 注册工作器
        builder.Services.AddSingleton<ParcelRoutingWorker>();
        builder.Services.AddHostedService<MainLineControlWorker>();
        builder.Services.AddHostedService<ParcelSortingSimulator>();
        builder.Services.AddHostedService<CartMovementSimulator>();
        builder.Services.AddHostedService<ParcelGeneratorWorker>();
        builder.Services.AddHostedService<OriginSensorMonitorHostedService>();
        builder.Services.AddHostedService<InfeedSensorMonitorHostedService>();

        // 注册 E2E Runner
        builder.Services.AddSingleton<EndToEndSimulationRunner>();

        // 配置日志（禁用日志以加快测试）
        builder.Logging.ClearProviders();
        builder.Logging.AddProvider(new TestLoggerProvider(_output));
        builder.Logging.SetMinimumLevel(LogLevel.Warning);

        var app = builder.Build();
        
        // 启用 setpoint
        var e2eSetpointProvider = app.Services.GetRequiredService<SimulationMainLineSetpoint>();
        e2eSetpointProvider.SetSetpoint(true, (decimal)simulationConfig.MainLineSpeedMmPerSec);
        
        // 获取 runner 实例（在 app.RunAsync 启动之前）
        var runner = app.Services.GetRequiredService<EndToEndSimulationRunner>();
        
        // 启动 fake 设备
        await fakeInfeedConveyor.StartAsync();
        await fakeInfeedConveyor.SetSpeedAsync(simulationConfig.InfeedConveyorSpeedMmPerSec);
        await fakeMainLineDrive.StartAsync();
        
        using var cts = new CancellationTokenSource();
        
        var e2eTask = Task.Run(async () =>
        {
            await Task.Delay(1000, cts.Token);
            
            var report = await runner.RunAsync(simulationConfig.ParcelCount, cts.Token);
            
            cts.Cancel();
            return report;
        }, cts.Token);
        
        var runTask = Task.Run(async () =>
        {
            try
            {
                await app.RunAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                // 正常结束
            }
        }, cts.Token);
        
        // 等待 E2E 任务完成
        var report = await e2eTask;
        
        try
        {
            await runTask;
        }
        catch (OperationCanceledException)
        {
            // 预期的取消
        }
        
        return report;
    }
}

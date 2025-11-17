using Xunit;
using Xunit.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Application;
using ZakYip.NarrowBeltDiverterSorter.Simulation;
using ZakYip.NarrowBeltDiverterSorter.Simulation.Fakes;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;

namespace ZakYip.NarrowBeltDiverterSorter.E2ETests;

/// <summary>
/// 测试安全场景仿真的输出结果
/// Tests safety scenario simulation output
/// </summary>
public class SafetyScenarioOutputTests
{
    private readonly ITestOutputHelper _output;

    public SafetyScenarioOutputTests(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// 测试格口安全重置场景的输出
    /// </summary>
    [Fact]
    public async Task ChuteReset_Safety_Scenario_ShouldProduceValidOutput()
    {
        // Arrange
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

        // Act
        var report = await RunSafetyScenarioAsync(numberOfChutes);

        // Assert - Verify safety scenario output is valid
        _output.WriteLine($"Safety scenario completed: {report.TotalChutes} chutes tested");
        
        // 1. 验证格口总数
        Assert.Equal(numberOfChutes, report.TotalChutes);
        
        // 2. 验证启动前已清零
        Assert.True(report.StartupCloseExecuted, "Startup close should be executed");
        
        // 3. 验证运行中有格口被触发
        Assert.True(report.ChutesTriggeredDuringRun > 0, 
            $"Some chutes should be triggered during run, but got {report.ChutesTriggeredDuringRun}");
        
        // 4. 验证停止后全部关闭
        Assert.Equal(0, report.ChutesOpenAfterShutdown);
        
        // 5. 验证没有异常情况
        Assert.Equal(0, report.ChutesOpenBeforeStartup);
        Assert.Equal(0, report.ChutesOpenAfterStartupClose);
        Assert.Equal(0, report.ChutesOpenAfterShutdown);
        
        // 6. 验证最终验证通过
        Assert.True(report.FinalVerificationPassed, "Final verification should pass");
        
        // 7. 验证没有错误信息
        Assert.True(string.IsNullOrEmpty(report.ErrorMessage), 
            $"No error message expected, but got: {report.ErrorMessage}");
        
        _output.WriteLine("✓ Safety scenario output is valid");
    }

    /// <summary>
    /// 测试多个格口的安全场景
    /// </summary>
    [Theory]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(15)]
    public async Task ChuteReset_WithDifferentChuteCount_ShouldProduceValidOutput(int chuteCount)
    {
        // Act
        var report = await RunSafetyScenarioAsync(chuteCount);

        // Assert
        _output.WriteLine($"Safety scenario with {chuteCount} chutes completed");
        
        Assert.Equal(chuteCount, report.TotalChutes);
        Assert.True(report.StartupCloseExecuted);
        Assert.Equal(0, report.ChutesOpenAfterShutdown);
        Assert.True(report.FinalVerificationPassed);
        
        _output.WriteLine($"✓ Safety scenario with {chuteCount} chutes is valid");
    }

    /// <summary>
    /// 运行安全场景仿真
    /// </summary>
    private async Task<SafetyScenarioReport> RunSafetyScenarioAsync(int numberOfChutes)
    {
        var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder();

        var simulationConfig = new SimulationConfiguration
        {
            NumberOfCarts = 20,
            CartSpacingMm = 500m,
            NumberOfChutes = numberOfChutes,
            ForceEjectChuteId = numberOfChutes, // 最后一个格口作为强排口
            MainLineSpeedMmPerSec = 1000.0,
            Scenario = "safety-chute-reset"
        };

        builder.Services.AddSingleton(simulationConfig);

        // 配置日志
        builder.Logging.ClearProviders();
        builder.Logging.AddProvider(new XunitLoggerProvider(_output));
        builder.Logging.SetMinimumLevel(LogLevel.Warning);

        // 注册 Fake 硬件
        var fakeChuteTransmitter = new FakeChuteTransmitterPort();
        builder.Services.AddSingleton(fakeChuteTransmitter);
        builder.Services.AddSingleton<IChuteTransmitterPort>(fakeChuteTransmitter);

        // 注册格口配置提供者
        builder.Services.AddSingleton<ZakYip.NarrowBeltDiverterSorter.Core.Domain.Sorting.IChuteConfigProvider>(sp =>
        {
            var provider = new ChuteConfigProvider();
            for (int i = 1; i <= numberOfChutes; i++)
            {
                provider.AddOrUpdate(new ZakYip.NarrowBeltDiverterSorter.Core.Domain.ChuteConfig
                {
                    ChuteId = new ZakYip.NarrowBeltDiverterSorter.Core.Domain.ChuteId(i),
                    IsEnabled = true,
                    IsForceEject = (i == simulationConfig.ForceEjectChuteId),
                    CartOffsetFromOrigin = i * 5,
                    MaxOpenDuration = TimeSpan.FromMilliseconds(300)
                });
            }
            return provider;
        });

        // 注册安全服务和场景运行器
        builder.Services.AddSingleton<IChuteSafetyService, SimulatedChuteSafetyService>();
        builder.Services.AddSingleton<SafetyScenarioRunner>();

        var app = builder.Build();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        
        var runner = app.Services.GetRequiredService<SafetyScenarioRunner>();
        var report = await runner.RunAsync(numberOfChutes, cts.Token);

        return report;
    }
}

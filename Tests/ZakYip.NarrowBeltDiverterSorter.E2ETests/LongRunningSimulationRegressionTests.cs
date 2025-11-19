using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Infrastructure.Simulation;

namespace ZakYip.NarrowBeltDiverterSorter.E2ETests;

/// <summary>
/// 长时间仿真回归测试（1000 包裹 / 300ms 间隔）
/// 验证仿真统计服务的核心功能
/// </summary>
[Trait("Category", "LongRunning")]
public class LongRunningSimulationRegressionTests
{
    private readonly ITestOutputHelper _output;

    public LongRunningSimulationRegressionTests(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// 测试仿真统计服务：记录 1000 个包裹的创建和分拣
    /// </summary>
    [Fact]
    public void SimulationStatisticsService_1000Parcels_AllSuccess()
    {
        // Arrange
        const int targetParcelCount = 1000;
        var statisticsService = new InMemorySimulationStatisticsService();
        var runId = $"lr-test-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";

        // Act - 模拟 1000 个包裹的处理
        statisticsService.StartRun(runId);
        
        for (int i = 1; i <= targetParcelCount; i++)
        {
            var parcelId = i;
            var targetChuteId = (i % 9) + 1; // 轮询格口 1-9
            
            // 记录包裹创建
            statisticsService.RecordParcelCreated(runId, parcelId);
            
            // 记录成功分拣
            statisticsService.RecordParcelSorted(runId, parcelId, targetChuteId, targetChuteId);
        }
        
        statisticsService.EndRun(runId);

        // Assert
        var statistics = statisticsService.GetStatistics(runId);
        Assert.NotNull(statistics);
        Assert.Equal(targetParcelCount, statistics.TotalParcels);
        Assert.Equal(targetParcelCount, statistics.SortedToTargetChutes);
        Assert.Equal(0, statistics.SortedToErrorChute);
        Assert.Equal(0, statistics.TimedOutCount);
        Assert.Equal(0, statistics.MisSortedCount);
        Assert.True(statistics.IsCompleted);

        _output.WriteLine($"✓ 统计服务测试通过：{statistics.TotalParcels} 个包裹全部成功分拣");
    }

    /// <summary>
    /// 测试超时场景：400 个包裹超时进入异常口
    /// </summary>
    [Fact]
    public void SimulationStatisticsService_1000Parcels_400Timeout()
    {
        // Arrange
        const int targetParcelCount = 1000;
        const int timeoutCount = 400;
        var statisticsService = new InMemorySimulationStatisticsService();
        var runId = $"lr-test-timeout-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";

        // Act - 模拟 1000 个包裹，其中 400 个超时
        statisticsService.StartRun(runId);
        
        for (int i = 1; i <= targetParcelCount; i++)
        {
            var parcelId = i;
            statisticsService.RecordParcelCreated(runId, parcelId);
            
            if (i <= timeoutCount)
            {
                // 前 400 个包裹超时并进入异常口
                statisticsService.RecordParcelTimedOut(runId, parcelId);
                statisticsService.RecordParcelToErrorChute(runId, parcelId);
            }
            else
            {
                // 后 600 个包裹正常分拣
                var targetChuteId = (i % 9) + 1;
                statisticsService.RecordParcelSorted(runId, parcelId, targetChuteId, targetChuteId);
            }
        }
        
        statisticsService.EndRun(runId);

        // Assert
        var statistics = statisticsService.GetStatistics(runId);
        Assert.NotNull(statistics);
        Assert.Equal(targetParcelCount, statistics.TotalParcels);
        Assert.Equal(targetParcelCount - timeoutCount, statistics.SortedToTargetChutes);
        Assert.Equal(timeoutCount, statistics.SortedToErrorChute);
        Assert.Equal(timeoutCount, statistics.TimedOutCount);
        Assert.Equal(0, statistics.MisSortedCount);
        Assert.True(statistics.IsCompleted);

        _output.WriteLine($"✓ 超时场景测试通过：{statistics.TimedOutCount} 个包裹超时并进入异常口");
    }

    /// <summary>
    /// 测试错分场景：验证错分统计
    /// </summary>
    [Fact]
    public void SimulationStatisticsService_1000Parcels_WithMissorts()
    {
        // Arrange
        const int targetParcelCount = 1000;
        const int missortCount = 50;
        var statisticsService = new InMemorySimulationStatisticsService();
        var runId = $"lr-test-missort-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";

        // Act - 模拟 1000 个包裹，其中 50 个错分
        statisticsService.StartRun(runId);
        
        for (int i = 1; i <= targetParcelCount; i++)
        {
            var parcelId = i;
            statisticsService.RecordParcelCreated(runId, parcelId);
            
            var targetChuteId = (i % 9) + 1;
            
            if (i <= missortCount)
            {
                // 前 50 个包裹错分到错误格口
                var wrongChuteId = (targetChuteId % 9) + 1; // 不同的格口
                statisticsService.RecordParcelSorted(runId, parcelId, targetChuteId, wrongChuteId);
            }
            else
            {
                // 其余包裹正常分拣
                statisticsService.RecordParcelSorted(runId, parcelId, targetChuteId, targetChuteId);
            }
        }
        
        statisticsService.EndRun(runId);

        // Assert
        var statistics = statisticsService.GetStatistics(runId);
        Assert.NotNull(statistics);
        Assert.Equal(targetParcelCount, statistics.TotalParcels);
        Assert.Equal(targetParcelCount - missortCount, statistics.SortedToTargetChutes);
        Assert.Equal(0, statistics.SortedToErrorChute);
        Assert.Equal(0, statistics.TimedOutCount);
        Assert.Equal(missortCount, statistics.MisSortedCount);
        Assert.True(statistics.IsCompleted);

        _output.WriteLine($"✓ 错分场景测试通过：{statistics.MisSortedCount} 个包裹被错分");
    }

    /// <summary>
    /// 测试混合场景：成功、超时、错分混合
    /// </summary>
    [Fact]
    public void SimulationStatisticsService_1000Parcels_MixedScenario()
    {
        // Arrange
        const int targetParcelCount = 1000;
        const int successCount = 850;
        const int timeoutCount = 100;
        const int missortCount = 50;
        var statisticsService = new InMemorySimulationStatisticsService();
        var runId = $"lr-test-mixed-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";

        // Act - 模拟混合场景
        statisticsService.StartRun(runId);
        
        int currentIndex = 1;
        
        // 850 个成功分拣
        for (int i = 0; i < successCount; i++)
        {
            var parcelId = currentIndex++;
            statisticsService.RecordParcelCreated(runId, parcelId);
            var targetChuteId = (parcelId % 9) + 1;
            statisticsService.RecordParcelSorted(runId, parcelId, targetChuteId, targetChuteId);
        }
        
        // 100 个超时
        for (int i = 0; i < timeoutCount; i++)
        {
            var parcelId = currentIndex++;
            statisticsService.RecordParcelCreated(runId, parcelId);
            statisticsService.RecordParcelTimedOut(runId, parcelId);
            statisticsService.RecordParcelToErrorChute(runId, parcelId);
        }
        
        // 50 个错分
        for (int i = 0; i < missortCount; i++)
        {
            var parcelId = currentIndex++;
            statisticsService.RecordParcelCreated(runId, parcelId);
            var targetChuteId = (parcelId % 9) + 1;
            var wrongChuteId = (targetChuteId % 9) + 1;
            statisticsService.RecordParcelSorted(runId, parcelId, targetChuteId, wrongChuteId);
        }
        
        statisticsService.EndRun(runId);

        // Assert
        var statistics = statisticsService.GetStatistics(runId);
        Assert.NotNull(statistics);
        Assert.Equal(targetParcelCount, statistics.TotalParcels);
        Assert.Equal(successCount, statistics.SortedToTargetChutes);
        Assert.Equal(timeoutCount, statistics.SortedToErrorChute);
        Assert.Equal(timeoutCount, statistics.TimedOutCount);
        Assert.Equal(missortCount, statistics.MisSortedCount);
        Assert.True(statistics.IsCompleted);

        _output.WriteLine($"✓ 混合场景测试通过：成功 {successCount}，超时 {timeoutCount}，错分 {missortCount}");
    }
}


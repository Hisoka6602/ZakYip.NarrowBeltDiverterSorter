using ZakYip.NarrowBeltDiverterSorter.Core.SelfCheck;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Tests.SelfCheck;

/// <summary>
/// ChuteCartMappingSelfCheckService 测试
/// </summary>
public class ChuteCartMappingSelfCheckServiceTests
{
    [Fact]
    public void Analyze_WithCorrectConfiguration_ShouldPass()
    {
        // Arrange
        var service = new ChuteCartMappingSelfCheckService();

        var topology = new TrackTopologySnapshot
        {
            CartCount = 20,
            CartSpacingMm = 500m,
            RingTotalLengthMm = 10000m,
            ChuteCount = 10,
            ChuteWidthMm = 500m,
            CartWidthMm = 200m,
            TrackLengthMm = 2500m  // 500 * 10 / 2 = 2500
        };

        var options = new ChuteCartMappingSelfCheckOptions
        {
            LoopCount = 5,
            CartIdTolerance = 0,
            PositionToleranceMm = 10m
        };

        // 模拟正确的格口-小车映射事件
        // 格口位置 = 格口宽度 * (格口编号 - 1)
        // 期望小车 = round(格口位置 / 小车节距) % 小车数量
        var events = new List<ChutePassEventArgs>();

        for (int loop = 0; loop < 5; loop++)
        {
            for (int chuteId = 1; chuteId <= 10; chuteId++)
            {
                var chutePositionMm = topology.ChuteWidthMm * (chuteId - 1);
                var expectedCartIndex = (int)Math.Round(chutePositionMm / topology.CartSpacingMm);
                var expectedCartId = expectedCartIndex % topology.CartCount;

                events.Add(new ChutePassEventArgs
                {
                    ChuteId = chuteId,
                    CartId = expectedCartId,
                    TriggeredAt = DateTimeOffset.UtcNow.AddSeconds(loop * 10 + chuteId),
                    LineSpeedMmps = 1000m
                });
            }
        }

        // Act
        var result = service.Analyze(events, topology, options);

        // Assert
        Assert.Equal(10, result.ChuteCount);
        Assert.Equal(20, result.CartCount);
        Assert.True(result.IsAllPassed, "所有格口应该通过验证");
        Assert.All(result.ChuteItems, item => Assert.True(item.IsPassed, $"格口 {item.ChuteId} 应该通过验证"));
        Assert.All(result.ChuteItems, item => Assert.Equal(5, item.ObservedCartIds.Count));
    }

    [Fact]
    public void Analyze_WithIncorrectMapping_ShouldFail()
    {
        // Arrange
        var service = new ChuteCartMappingSelfCheckService();

        var topology = new TrackTopologySnapshot
        {
            CartCount = 20,
            CartSpacingMm = 500m,
            RingTotalLengthMm = 10000m,
            ChuteCount = 10,
            ChuteWidthMm = 500m,
            CartWidthMm = 200m,
            TrackLengthMm = 2500m
        };

        var options = new ChuteCartMappingSelfCheckOptions
        {
            LoopCount = 3,
            CartIdTolerance = 0,
            PositionToleranceMm = 10m
        };

        // 模拟错误的格口-小车映射（使用错误的小车编号）
        var events = new List<ChutePassEventArgs>();

        for (int loop = 0; loop < 3; loop++)
        {
            for (int chuteId = 1; chuteId <= 10; chuteId++)
            {
                // 故意使用错误的小车编号（+1偏移）
                var chutePositionMm = topology.ChuteWidthMm * (chuteId - 1);
                var expectedCartIndex = (int)Math.Round(chutePositionMm / topology.CartSpacingMm);
                var wrongCartId = (expectedCartIndex + 1) % topology.CartCount;

                events.Add(new ChutePassEventArgs
                {
                    ChuteId = chuteId,
                    CartId = wrongCartId,  // 错误的小车编号
                    TriggeredAt = DateTimeOffset.UtcNow.AddSeconds(loop * 10 + chuteId),
                    LineSpeedMmps = 1000m
                });
            }
        }

        // Act
        var result = service.Analyze(events, topology, options);

        // Assert
        Assert.Equal(10, result.ChuteCount);
        Assert.Equal(20, result.CartCount);
        Assert.False(result.IsAllPassed, "应该检测到映射错误");
        Assert.All(result.ChuteItems, item => Assert.False(item.IsPassed, $"格口 {item.ChuteId} 应该失败"));
    }

    [Fact]
    public void Analyze_WithTolerance_ShouldPassWithinTolerance()
    {
        // Arrange
        var service = new ChuteCartMappingSelfCheckService();

        var topology = new TrackTopologySnapshot
        {
            CartCount = 20,
            CartSpacingMm = 500m,
            RingTotalLengthMm = 10000m,
            ChuteCount = 10,
            ChuteWidthMm = 500m,
            CartWidthMm = 200m,
            TrackLengthMm = 2500m
        };

        var options = new ChuteCartMappingSelfCheckOptions
        {
            LoopCount = 3,
            CartIdTolerance = 1,  // 允许1辆车的容差
            PositionToleranceMm = 10m
        };

        // 模拟轻微偏移的格口-小车映射（在容差范围内）
        var events = new List<ChutePassEventArgs>();

        for (int loop = 0; loop < 3; loop++)
        {
            for (int chuteId = 1; chuteId <= 10; chuteId++)
            {
                var chutePositionMm = topology.ChuteWidthMm * (chuteId - 1);
                var expectedCartIndex = (int)Math.Round(chutePositionMm / topology.CartSpacingMm);
                var cartIdWithOffset = (expectedCartIndex + (loop % 2)) % topology.CartCount;  // 轻微偏移

                events.Add(new ChutePassEventArgs
                {
                    ChuteId = chuteId,
                    CartId = cartIdWithOffset,
                    TriggeredAt = DateTimeOffset.UtcNow.AddSeconds(loop * 10 + chuteId),
                    LineSpeedMmps = 1000m
                });
            }
        }

        // Act
        var result = service.Analyze(events, topology, options);

        // Assert
        Assert.Equal(10, result.ChuteCount);
        Assert.True(result.IsAllPassed, "在容差范围内应该通过验证");
    }

    [Fact]
    public void Analyze_WithNoEvents_ShouldFail()
    {
        // Arrange
        var service = new ChuteCartMappingSelfCheckService();

        var topology = new TrackTopologySnapshot
        {
            CartCount = 20,
            CartSpacingMm = 500m,
            RingTotalLengthMm = 10000m,
            ChuteCount = 10,
            ChuteWidthMm = 500m,
            CartWidthMm = 200m,
            TrackLengthMm = 2500m
        };

        var options = new ChuteCartMappingSelfCheckOptions
        {
            LoopCount = 3,
            CartIdTolerance = 0,
            PositionToleranceMm = 10m
        };

        var events = new List<ChutePassEventArgs>();  // 没有事件

        // Act
        var result = service.Analyze(events, topology, options);

        // Assert
        Assert.Equal(10, result.ChuteCount);
        Assert.False(result.IsAllPassed, "没有事件应该失败");
        Assert.All(result.ChuteItems, item => Assert.False(item.IsPassed));
        Assert.All(result.ChuteItems, item => Assert.Empty(item.ObservedCartIds));
    }

    [Fact]
    public void Analyze_ShouldThrowArgumentNullException_ForNullEvents()
    {
        // Arrange
        var service = new ChuteCartMappingSelfCheckService();

        var topology = new TrackTopologySnapshot
        {
            CartCount = 20,
            CartSpacingMm = 500m,
            RingTotalLengthMm = 10000m,
            ChuteCount = 10,
            ChuteWidthMm = 500m,
            CartWidthMm = 200m,
            TrackLengthMm = 2500m
        };

        var options = new ChuteCartMappingSelfCheckOptions
        {
            LoopCount = 3,
            CartIdTolerance = 0,
            PositionToleranceMm = 10m
        };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => service.Analyze(null!, topology, options));
    }

    [Fact]
    public void Analyze_ShouldThrowArgumentNullException_ForNullTopology()
    {
        // Arrange
        var service = new ChuteCartMappingSelfCheckService();

        var events = new List<ChutePassEventArgs>();

        var options = new ChuteCartMappingSelfCheckOptions
        {
            LoopCount = 3,
            CartIdTolerance = 0,
            PositionToleranceMm = 10m
        };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => service.Analyze(events, null!, options));
    }

    [Fact]
    public void Analyze_ShouldThrowArgumentNullException_ForNullOptions()
    {
        // Arrange
        var service = new ChuteCartMappingSelfCheckService();

        var topology = new TrackTopologySnapshot
        {
            CartCount = 20,
            CartSpacingMm = 500m,
            RingTotalLengthMm = 10000m,
            ChuteCount = 10,
            ChuteWidthMm = 500m,
            CartWidthMm = 200m,
            TrackLengthMm = 2500m
        };

        var events = new List<ChutePassEventArgs>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => service.Analyze(events, topology, null!));
    }
}

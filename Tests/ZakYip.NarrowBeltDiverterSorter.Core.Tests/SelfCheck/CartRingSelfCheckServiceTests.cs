using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Tracking;
using ZakYip.NarrowBeltDiverterSorter.Core.SelfCheck;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Tests.SelfCheck;

/// <summary>
/// CartRingSelfCheckService测试
/// </summary>
public class CartRingSelfCheckServiceTests
{
    [Fact]
    public void RunAnalysis_Should_Return_Matched_Result_For_Correct_Configuration()
    {
        // Arrange
        var options = new CartRingSelfCheckOptions
        {
            PitchTolerancePercent = 0.05 // 5% tolerance
        };
        var service = new CartRingSelfCheckService(options);

        var topology = new TrackTopologySnapshot
        {
            CartCount = 20,
            CartSpacingMm = 500m,
            RingTotalLengthMm = 10000m,
            ChuteCount = 32,
            ChuteWidthMm = 500m,
            CartWidthMm = 200m,
            TrackLengthMm = 8000m
        };

        // 模拟20辆小车通过，速度1000mm/s，节距500mm
        var baseTime = DateTimeOffset.Now;
        var events = new List<CartPassEventArgs>();
        for (int i = 0; i < 20; i++)
        {
            events.Add(new CartPassEventArgs
            {
                CartId = i,
                PassAt = baseTime.AddMilliseconds(i * 500), // 500ms间隔
                LineSpeedMmps = 1000m // 1000mm/s * 0.5s = 500mm
            });
        }

        // Act
        var result = service.RunAnalysis(events, topology);

        // Assert
        Assert.Equal(20, result.ExpectedCartCount);
        Assert.Equal(20, result.MeasuredCartCount);
        Assert.Equal(500m, result.ExpectedPitchMm);
        Assert.True(result.IsCartCountMatched);
        Assert.True(result.IsPitchWithinTolerance);
        
        // 验证测量的节距在合理范围内
        Assert.True(result.MeasuredPitchMm > 475m && result.MeasuredPitchMm < 525m);
    }

    [Fact]
    public void RunAnalysis_Should_Return_Unmatched_For_Incorrect_Cart_Count()
    {
        // Arrange
        var options = new CartRingSelfCheckOptions();
        var service = new CartRingSelfCheckService(options);

        var topology = new TrackTopologySnapshot
        {
            CartCount = 20,
            CartSpacingMm = 500m,
            RingTotalLengthMm = 10000m,
            ChuteCount = 32,
            ChuteWidthMm = 500m,
            CartWidthMm = 200m,
            TrackLengthMm = 8000m
        };

        // 模拟只有18辆小车通过（配置是20辆）
        var baseTime = DateTimeOffset.Now;
        var events = new List<CartPassEventArgs>();
        for (int i = 0; i < 18; i++)
        {
            events.Add(new CartPassEventArgs
            {
                CartId = i,
                PassAt = baseTime.AddMilliseconds(i * 500),
                LineSpeedMmps = 1000m
            });
        }

        // Act
        var result = service.RunAnalysis(events, topology);

        // Assert
        Assert.Equal(20, result.ExpectedCartCount);
        Assert.Equal(18, result.MeasuredCartCount);
        Assert.False(result.IsCartCountMatched);
    }

    [Fact]
    public void RunAnalysis_Should_Detect_Incorrect_Pitch()
    {
        // Arrange
        var options = new CartRingSelfCheckOptions
        {
            PitchTolerancePercent = 0.05 // 5% tolerance
        };
        var service = new CartRingSelfCheckService(options);

        var topology = new TrackTopologySnapshot
        {
            CartCount = 20,
            CartSpacingMm = 500m,
            RingTotalLengthMm = 10000m,
            ChuteCount = 32,
            ChuteWidthMm = 500m,
            CartWidthMm = 200m,
            TrackLengthMm = 8000m
        };

        // 模拟20辆小车，但实际节距是550mm（超出5%容差）
        var baseTime = DateTimeOffset.Now;
        var events = new List<CartPassEventArgs>();
        for (int i = 0; i < 20; i++)
        {
            events.Add(new CartPassEventArgs
            {
                CartId = i,
                PassAt = baseTime.AddMilliseconds(i * 550), // 550ms间隔
                LineSpeedMmps = 1000m // 1000mm/s * 0.55s = 550mm
            });
        }

        // Act
        var result = service.RunAnalysis(events, topology);

        // Assert
        Assert.Equal(20, result.ExpectedCartCount);
        Assert.Equal(20, result.MeasuredCartCount);
        Assert.True(result.IsCartCountMatched);
        Assert.False(result.IsPitchWithinTolerance);
        
        // 验证测量的节距接近550mm
        Assert.True(result.MeasuredPitchMm > 520m && result.MeasuredPitchMm < 580m);
    }

    [Fact]
    public void RunAnalysis_Should_Handle_Variable_Speed()
    {
        // Arrange
        var options = new CartRingSelfCheckOptions
        {
            PitchTolerancePercent = 0.10 // 10% tolerance for variable speed
        };
        var service = new CartRingSelfCheckService(options);

        var topology = new TrackTopologySnapshot
        {
            CartCount = 10,
            CartSpacingMm = 500m,
            RingTotalLengthMm = 5000m,
            ChuteCount = 16,
            ChuteWidthMm = 500m,
            CartWidthMm = 200m,
            TrackLengthMm = 4000m
        };

        // 模拟速度轻微变化的场景（更接近实际）
        var baseTime = DateTimeOffset.Now;
        var events = new List<CartPassEventArgs>();
        var speeds = new[] { 950m, 980m, 1000m, 1020m, 1000m, 990m, 1010m, 1000m, 970m, 1000m };
        
        var currentTime = baseTime;
        for (int i = 0; i < 10; i++)
        {
            events.Add(new CartPassEventArgs
            {
                CartId = i,
                PassAt = currentTime,
                LineSpeedMmps = speeds[i]
            });
            
            // 计算下一个小车到达时间：节距 / 速度
            var intervalSeconds = 500m / speeds[i];
            currentTime = currentTime.AddSeconds((double)intervalSeconds);
        }

        // Act
        var result = service.RunAnalysis(events, topology);

        // Assert
        Assert.Equal(10, result.MeasuredCartCount);
        Assert.True(result.IsCartCountMatched);
        
        // 节距应该在500mm左右，允许更大的误差因为速度变化
        Assert.True(result.MeasuredPitchMm > 450m && result.MeasuredPitchMm < 550m);
    }

    [Fact]
    public void RunAnalysis_Should_Return_Empty_Result_For_No_Events()
    {
        // Arrange
        var options = new CartRingSelfCheckOptions();
        var service = new CartRingSelfCheckService(options);

        var topology = new TrackTopologySnapshot
        {
            CartCount = 20,
            CartSpacingMm = 500m,
            RingTotalLengthMm = 10000m,
            ChuteCount = 32,
            ChuteWidthMm = 500m,
            CartWidthMm = 200m,
            TrackLengthMm = 8000m
        };

        var events = new List<CartPassEventArgs>();

        // Act
        var result = service.RunAnalysis(events, topology);

        // Assert
        Assert.Equal(20, result.ExpectedCartCount);
        Assert.Equal(0, result.MeasuredCartCount);
        Assert.Equal(0m, result.MeasuredPitchMm);
        Assert.False(result.IsCartCountMatched);
        Assert.False(result.IsPitchWithinTolerance);
    }

    [Fact]
    public void RunAnalysis_Should_Handle_Duplicate_Cart_Ids()
    {
        // Arrange
        var options = new CartRingSelfCheckOptions();
        var service = new CartRingSelfCheckService(options);

        var topology = new TrackTopologySnapshot
        {
            CartCount = 5,
            CartSpacingMm = 500m,
            RingTotalLengthMm = 2500m,
            ChuteCount = 8,
            ChuteWidthMm = 500m,
            CartWidthMm = 200m,
            TrackLengthMm = 2000m
        };

        // 模拟多圈数据，包含重复的小车ID
        var baseTime = DateTimeOffset.Now;
        var events = new List<CartPassEventArgs>();
        
        // 第一圈
        for (int i = 0; i < 5; i++)
        {
            events.Add(new CartPassEventArgs
            {
                CartId = i,
                PassAt = baseTime.AddMilliseconds(i * 500),
                LineSpeedMmps = 1000m
            });
        }
        
        // 第二圈
        for (int i = 0; i < 5; i++)
        {
            events.Add(new CartPassEventArgs
            {
                CartId = i,
                PassAt = baseTime.AddMilliseconds(2500 + i * 500),
                LineSpeedMmps = 1000m
            });
        }

        // Act
        var result = service.RunAnalysis(events, topology);

        // Assert
        Assert.Equal(5, result.MeasuredCartCount); // 应该只统计唯一的小车数
        Assert.True(result.IsCartCountMatched);
    }

    [Fact]
    public void RunAnalysis_Should_Throw_ArgumentNullException_For_Null_Events()
    {
        // Arrange
        var options = new CartRingSelfCheckOptions();
        var service = new CartRingSelfCheckService(options);
        var topology = new TrackTopologySnapshot
        {
            CartCount = 20,
            CartSpacingMm = 500m,
            RingTotalLengthMm = 10000m,
            ChuteCount = 32,
            ChuteWidthMm = 500m,
            CartWidthMm = 200m,
            TrackLengthMm = 8000m
        };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            service.RunAnalysis(null!, topology));
    }

    [Fact]
    public void RunAnalysis_Should_Throw_ArgumentNullException_For_Null_Topology()
    {
        // Arrange
        var options = new CartRingSelfCheckOptions();
        var service = new CartRingSelfCheckService(options);
        var events = new List<CartPassEventArgs>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            service.RunAnalysis(events, null!));
    }
}

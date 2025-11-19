using Xunit;
using Xunit.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Tracking;
using ZakYip.NarrowBeltDiverterSorter.Core.SelfCheck;

namespace ZakYip.NarrowBeltDiverterSorter.E2ETests;

/// <summary>
/// 小车环自检E2E测试
/// </summary>
public class CartRingSelfCheckTests
{
    private readonly ITestOutputHelper _output;

    public CartRingSelfCheckTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void CartSelfCheck_WithCorrectConfiguration_ShouldPass()
    {
        // Arrange - 使用正确的配置
        var selfCheckOptions = new CartRingSelfCheckOptions
        {
            PitchTolerancePercent = 0.05,
            MinCompleteRings = 2
        };
        var service = new CartRingSelfCheckService(selfCheckOptions);

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

        // 模拟完整的2圈数据（20辆小车 × 2圈 = 40个事件）
        var baseTime = DateTimeOffset.UtcNow;
        var events = new List<CartPassEventArgs>();
        
        for (int ring = 0; ring < 2; ring++)
        {
            for (int i = 0; i < 20; i++)
            {
                events.Add(new CartPassEventArgs
                {
                    CartId = i,
                    PassAt = baseTime.AddMilliseconds((ring * 20 + i) * 500),
                    LineSpeedMmps = 1000m // 1000mm/s * 0.5s = 500mm
                });
            }
        }

        // Act
        var result = service.RunAnalysis(events, topology);

        // Assert
        _output.WriteLine("=== 小车环自检结果 ===");
        _output.WriteLine($"配置小车数: {result.ExpectedCartCount} 辆");
        _output.WriteLine($"检测小车数: {result.MeasuredCartCount} 辆");
        _output.WriteLine($"配置节距: {result.ExpectedPitchMm:F1} mm");
        _output.WriteLine($"估算节距: {result.MeasuredPitchMm:F1} mm");
        _output.WriteLine($"数车结果: {(result.IsCartCountMatched ? "✓ 通过" : "✗ 不匹配")}");
        _output.WriteLine($"节距结果: {(result.IsPitchWithinTolerance ? "✓ 在误差范围内" : "✗ 超出误差范围")}");

        Assert.Equal(20, result.ExpectedCartCount);
        Assert.Equal(20, result.MeasuredCartCount);
        Assert.True(result.IsCartCountMatched, "小车数量应该匹配");
        Assert.True(result.IsPitchWithinTolerance, "节距应该在容忍范围内");
        
        // 验证测量的节距接近配置值
        var deviation = Math.Abs(result.MeasuredPitchMm - result.ExpectedPitchMm);
        var deviationPercent = deviation / result.ExpectedPitchMm;
        Assert.True(deviationPercent <= 0.05m, $"节距偏差应该小于5%，实际: {deviationPercent * 100:F2}%");
    }

    [Fact]
    public void CartSelfCheck_WithIncorrectCartCount_ShouldFail()
    {
        // Arrange - 配置小车数量错误（实际有20辆，但配置成18辆）
        var selfCheckOptions = new CartRingSelfCheckOptions
        {
            PitchTolerancePercent = 0.05
        };
        var service = new CartRingSelfCheckService(selfCheckOptions);

        // 模拟配置18辆，但实际检测到20辆
        var topology = new TrackTopologySnapshot
        {
            CartCount = 18, // 错误的配置
            CartSpacingMm = 500m,
            RingTotalLengthMm = 9000m,
            ChuteCount = 29,
            ChuteWidthMm = 500m,
            CartWidthMm = 200m,
            TrackLengthMm = 7250m
        };

        // 模拟20辆小车通过
        var baseTime = DateTimeOffset.UtcNow;
        var events = new List<CartPassEventArgs>();
        for (int i = 0; i < 20; i++)
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
        _output.WriteLine("=== 小车环自检结果（错误配置） ===");
        _output.WriteLine($"配置小车数: {result.ExpectedCartCount} 辆");
        _output.WriteLine($"检测小车数: {result.MeasuredCartCount} 辆");
        _output.WriteLine($"数车结果: {(result.IsCartCountMatched ? "✓ 通过" : "✗ 不匹配")}");

        Assert.Equal(18, result.ExpectedCartCount);
        Assert.Equal(20, result.MeasuredCartCount);
        Assert.False(result.IsCartCountMatched, "小车数量应该不匹配");
    }

    [Fact]
    public void CartSelfCheck_WithIncorrectPitch_ShouldFail()
    {
        // Arrange - 节距配置错误
        var selfCheckOptions = new CartRingSelfCheckOptions
        {
            PitchTolerancePercent = 0.05 // 5% 容差
        };
        var service = new CartRingSelfCheckService(selfCheckOptions);

        var topology = new TrackTopologySnapshot
        {
            CartCount = 20,
            CartSpacingMm = 500m, // 配置是500mm
            RingTotalLengthMm = 10000m,
            ChuteCount = 32,
            ChuteWidthMm = 500m,
            CartWidthMm = 200m,
            TrackLengthMm = 8000m
        };

        // 模拟实际节距是550mm（超出5%容差）
        var baseTime = DateTimeOffset.UtcNow;
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
        _output.WriteLine("=== 小车环自检结果（节距错误） ===");
        _output.WriteLine($"配置节距: {result.ExpectedPitchMm:F1} mm");
        _output.WriteLine($"估算节距: {result.MeasuredPitchMm:F1} mm");
        _output.WriteLine($"节距结果: {(result.IsPitchWithinTolerance ? "✓ 在误差范围内" : "✗ 超出误差范围")}");

        Assert.Equal(20, result.MeasuredCartCount);
        Assert.True(result.IsCartCountMatched, "小车数量应该匹配");
        Assert.False(result.IsPitchWithinTolerance, "节距应该超出容忍范围");
        
        // 验证测量的节距确实接近550mm
        Assert.True(result.MeasuredPitchMm > 520m && result.MeasuredPitchMm < 580m);
    }
}

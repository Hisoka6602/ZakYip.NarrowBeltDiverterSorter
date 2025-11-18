using Microsoft.Extensions.Options;
using Moq;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.MainLine;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Execution.MainLine;

namespace ZakYip.NarrowBeltDiverterSorter.Execution.Tests;

/// <summary>
/// 主线速度提供者测试
/// </summary>
public class MainLineSpeedProviderTests
{
    private readonly Mock<IMainLineFeedbackPort> _mockFeedbackPort;
    private readonly MainLineControlOptions _options;

    public MainLineSpeedProviderTests()
    {
        _mockFeedbackPort = new Mock<IMainLineFeedbackPort>();
        _options = new MainLineControlOptions
        {
            TargetSpeedMmps = 1000m,
            LoopPeriod = TimeSpan.FromMilliseconds(100),
            StableDeadbandMmps = 10m,
            StableHold = TimeSpan.FromSeconds(2)
        };
    }

    private MainLineSpeedProvider CreateProvider()
    {
        return new MainLineSpeedProvider(
            _mockFeedbackPort.Object,
            Options.Create(_options));
    }

    [Fact]
    public void CurrentMmps_Should_Return_Smoothed_Speed()
    {
        // Arrange
        var provider = CreateProvider();
        _mockFeedbackPort.SetupSequence(x => x.GetCurrentSpeed())
            .Returns(1000.0)
            .Returns(1010.0)
            .Returns(990.0);

        // Act
        var speed1 = provider.CurrentMmps;
        var speed2 = provider.CurrentMmps;
        var speed3 = provider.CurrentMmps;

        // Assert
        Assert.Equal(1000m, speed1);
        Assert.Equal(1005m, speed2); // Average of 1000 and 1010
        Assert.Equal(1000m, speed3); // Average of 1000, 1010, and 990
    }

    [Fact]
    public void IsSpeedStable_Should_Return_False_When_Error_Exceeds_Deadband()
    {
        // Arrange
        var provider = CreateProvider();
        _mockFeedbackPort.Setup(x => x.GetCurrentSpeed())
            .Returns(950.0); // 50 mm/s error, exceeds 10 mm/s deadband

        // Act
        var isStable = provider.IsSpeedStable;

        // Assert
        Assert.False(isStable);
    }

    [Fact]
    public void IsSpeedStable_Should_Return_True_After_Hold_Period()
    {
        // Arrange
        var provider = CreateProvider();
        _mockFeedbackPort.Setup(x => x.GetCurrentSpeed())
            .Returns(995.0); // 5 mm/s error, within 10 mm/s deadband

        // Act - First check sets the stable start time
        var isStable1 = provider.IsSpeedStable;
        Assert.False(isStable1); // Should be false initially due to hold time

        // Wait for stability hold period
        Thread.Sleep(2100); // Wait slightly more than 2 seconds

        // Act - Second check after hold period
        var isStable2 = provider.IsSpeedStable;

        // Assert
        Assert.True(isStable2);
    }

    [Fact]
    public void IsSpeedStable_Should_Reset_When_Leaving_Stable_Range()
    {
        // Arrange
        var provider = CreateProvider();
        _mockFeedbackPort.SetupSequence(x => x.GetCurrentSpeed())
            .Returns(995.0)  // Within deadband
            .Returns(950.0); // Outside deadband

        // Act
        var isStable1 = provider.IsSpeedStable;
        var isStable2 = provider.IsSpeedStable;

        // Assert
        Assert.False(isStable1); // Not stable yet (hold time not elapsed)
        Assert.False(isStable2); // Reset due to leaving stable range
    }

    [Fact]
    public void StableDuration_Should_Return_Zero_When_Not_Stable()
    {
        // Arrange
        var provider = CreateProvider();
        _mockFeedbackPort.Setup(x => x.GetCurrentSpeed())
            .Returns(950.0); // Outside deadband

        // Act
        var isStable = provider.IsSpeedStable;
        var duration = provider.StableDuration;

        // Assert
        Assert.False(isStable);
        Assert.Equal(TimeSpan.Zero, duration);
    }

    [Fact]
    public void StableDuration_Should_Increase_When_Stable()
    {
        // Arrange
        var provider = CreateProvider();
        _mockFeedbackPort.Setup(x => x.GetCurrentSpeed())
            .Returns(995.0); // Within deadband

        // Act
        var isStable1 = provider.IsSpeedStable; // Start stable period
        Thread.Sleep(100);
        var duration = provider.StableDuration;

        // Assert
        Assert.True(duration > TimeSpan.Zero);
        Assert.True(duration < TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ResetSmoothing_Should_Clear_Samples()
    {
        // Arrange
        var provider = CreateProvider();
        _mockFeedbackPort.Setup(x => x.GetCurrentSpeed())
            .Returns(1000.0);

        // Build up some samples
        var speed1 = provider.CurrentMmps;
        var speed2 = provider.CurrentMmps;

        // Act
        provider.ResetSmoothing();
        var speedAfterReset = provider.CurrentMmps;

        // Assert
        Assert.Equal(1000m, speedAfterReset);
        Assert.Equal(TimeSpan.Zero, provider.StableDuration);
    }
}

using Microsoft.Extensions.Logging;
using Moq;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Feeding;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Parcels;
using ZakYip.NarrowBeltDiverterSorter.Execution.Feeding;

namespace ZakYip.NarrowBeltDiverterSorter.Execution.Tests.Feeding;

/// <summary>
/// 供包背压控制器测试
/// </summary>
public class FeedingBackpressureControllerTests
{
    private readonly Mock<IFeedingCapacityOptionsRepository> _mockRepository;
    private readonly Mock<IParcelLifecycleTracker> _mockTracker;
    private readonly Mock<ILogger<FeedingBackpressureController>> _mockLogger;

    public FeedingBackpressureControllerTests()
    {
        _mockRepository = new Mock<IFeedingCapacityOptionsRepository>();
        _mockTracker = new Mock<IParcelLifecycleTracker>();
        _mockLogger = new Mock<ILogger<FeedingBackpressureController>>();
    }

    private FeedingBackpressureController CreateController(FeedingCapacityOptions? options = null)
    {
        options ??= new FeedingCapacityOptions
        {
            MaxInFlightParcels = 100,
            MaxUpstreamPendingRequests = 10,
            ThrottleMode = FeedingThrottleMode.None
        };

        _mockRepository.Setup(x => x.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(options);

        return new FeedingBackpressureController(
            _mockRepository.Object,
            _mockTracker.Object,
            _mockLogger.Object);
    }

    [Fact]
    public void CheckFeedingAllowed_WhenBelowLimits_ShouldAllow()
    {
        // Arrange
        var controller = CreateController();
        _mockTracker.Setup(x => x.GetInFlightCount()).Returns(50);
        _mockTracker.Setup(x => x.GetUpstreamPendingCount()).Returns(5);

        // Act
        var result = controller.CheckFeedingAllowed();

        // Assert
        Assert.Equal(FeedingDecision.Allow, result.Decision);
        Assert.Equal(50, result.CurrentInFlightCount);
        Assert.Equal(5, result.CurrentUpstreamPendingCount);
    }

    [Fact]
    public void CheckFeedingAllowed_WhenExceedsInFlightLimit_WithPauseMode_ShouldReject()
    {
        // Arrange
        var options = new FeedingCapacityOptions
        {
            MaxInFlightParcels = 100,
            MaxUpstreamPendingRequests = 10,
            ThrottleMode = FeedingThrottleMode.Pause
        };
        var controller = CreateController(options);
        _mockTracker.Setup(x => x.GetInFlightCount()).Returns(100);
        _mockTracker.Setup(x => x.GetUpstreamPendingCount()).Returns(5);

        // Act
        var result = controller.CheckFeedingAllowed();

        // Assert
        Assert.Equal(FeedingDecision.Reject, result.Decision);
        Assert.Equal(100, result.CurrentInFlightCount);
        Assert.Contains("在途包裹数", result.Reason);
    }

    [Fact]
    public void CheckFeedingAllowed_WhenExceedsInFlightLimit_WithSlowDownMode_ShouldDelay()
    {
        // Arrange
        var options = new FeedingCapacityOptions
        {
            MaxInFlightParcels = 100,
            MaxUpstreamPendingRequests = 10,
            ThrottleMode = FeedingThrottleMode.SlowDown
        };
        var controller = CreateController(options);
        _mockTracker.Setup(x => x.GetInFlightCount()).Returns(100);
        _mockTracker.Setup(x => x.GetUpstreamPendingCount()).Returns(5);

        // Act
        var result = controller.CheckFeedingAllowed();

        // Assert
        Assert.Equal(FeedingDecision.Delay, result.Decision);
        Assert.NotNull(result.SuggestedDelayMs);
        Assert.True(result.SuggestedDelayMs > 0);
    }

    [Fact]
    public void CheckFeedingAllowed_WhenExceedsUpstreamLimit_WithPauseMode_ShouldReject()
    {
        // Arrange
        var options = new FeedingCapacityOptions
        {
            MaxInFlightParcels = 100,
            MaxUpstreamPendingRequests = 10,
            ThrottleMode = FeedingThrottleMode.Pause
        };
        var controller = CreateController(options);
        _mockTracker.Setup(x => x.GetInFlightCount()).Returns(50);
        _mockTracker.Setup(x => x.GetUpstreamPendingCount()).Returns(10);

        // Act
        var result = controller.CheckFeedingAllowed();

        // Assert
        Assert.Equal(FeedingDecision.Reject, result.Decision);
        Assert.Equal(10, result.CurrentUpstreamPendingCount);
        Assert.Contains("上游等待数", result.Reason);
    }

    [Fact]
    public void CheckFeedingAllowed_WhenExceedsLimit_WithNoneMode_ShouldAllowWithWarning()
    {
        // Arrange
        var options = new FeedingCapacityOptions
        {
            MaxInFlightParcels = 100,
            MaxUpstreamPendingRequests = 10,
            ThrottleMode = FeedingThrottleMode.None
        };
        var controller = CreateController(options);
        _mockTracker.Setup(x => x.GetInFlightCount()).Returns(100);
        _mockTracker.Setup(x => x.GetUpstreamPendingCount()).Returns(5);

        // Act
        var result = controller.CheckFeedingAllowed();

        // Assert
        Assert.Equal(FeedingDecision.Allow, result.Decision);
        Assert.Contains("仅告警", result.Reason);
    }

    [Fact]
    public void RecordThrottleEvent_ShouldIncrementCounter()
    {
        // Arrange
        var controller = CreateController();

        // Act
        controller.RecordThrottleEvent();
        controller.RecordThrottleEvent();
        var count = controller.GetThrottleCount();

        // Assert
        Assert.Equal(2, count);
    }

    [Fact]
    public void RecordPauseEvent_ShouldIncrementCounter()
    {
        // Arrange
        var controller = CreateController();

        // Act
        controller.RecordPauseEvent();
        controller.RecordPauseEvent();
        controller.RecordPauseEvent();
        var count = controller.GetPauseCount();

        // Assert
        Assert.Equal(3, count);
    }

    [Fact]
    public void ResetCounters_ShouldResetBothCounters()
    {
        // Arrange
        var controller = CreateController();
        controller.RecordThrottleEvent();
        controller.RecordPauseEvent();

        // Act
        controller.ResetCounters();

        // Assert
        Assert.Equal(0, controller.GetThrottleCount());
        Assert.Equal(0, controller.GetPauseCount());
    }

    [Fact]
    public void CheckFeedingAllowed_NearRecoveryThreshold_WithSlowDownMode_ShouldDelay()
    {
        // Arrange
        var options = new FeedingCapacityOptions
        {
            MaxInFlightParcels = 100,
            MaxUpstreamPendingRequests = 10,
            ThrottleMode = FeedingThrottleMode.SlowDown,
            RecoveryThreshold = 80
        };
        var controller = CreateController(options);
        _mockTracker.Setup(x => x.GetInFlightCount()).Returns(85);
        _mockTracker.Setup(x => x.GetUpstreamPendingCount()).Returns(5);

        // Act
        var result = controller.CheckFeedingAllowed();

        // Assert
        Assert.Equal(FeedingDecision.Delay, result.Decision);
        Assert.Contains("接近限制", result.Reason);
    }
}

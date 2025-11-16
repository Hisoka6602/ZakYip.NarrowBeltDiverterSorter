using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Tracking;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Tests.Tracking;

/// <summary>
/// 小车位置跟踪器测试
/// </summary>
public class CartPositionTrackerTests
{
    [Fact]
    public void OnCartPassedOrigin_FirstCall_ShouldSetCurrentOriginCartIndexToZero()
    {
        // Arrange
        var tracker = new CartPositionTracker();

        // Act
        tracker.OnCartPassedOrigin(DateTimeOffset.UtcNow);

        // Assert
        Assert.NotNull(tracker.CurrentOriginCartIndex);
        Assert.Equal(0, tracker.CurrentOriginCartIndex.Value.Value);
    }

    [Fact]
    public void OnCartPassedOrigin_MultipleCalls_ShouldIncrementCartIndex()
    {
        // Arrange
        var tracker = new CartPositionTracker();

        // Act
        tracker.OnCartPassedOrigin(DateTimeOffset.UtcNow);
        tracker.OnCartPassedOrigin(DateTimeOffset.UtcNow);
        tracker.OnCartPassedOrigin(DateTimeOffset.UtcNow);

        // Assert
        Assert.NotNull(tracker.CurrentOriginCartIndex);
        Assert.Equal(2, tracker.CurrentOriginCartIndex.Value.Value);
    }

    [Fact]
    public void CalculateCartIndexAtOffset_WithZeroOffset_ShouldReturnCurrentOriginCart()
    {
        // Arrange
        var tracker = new CartPositionTracker();
        tracker.OnCartPassedOrigin(DateTimeOffset.UtcNow);
        tracker.OnCartPassedOrigin(DateTimeOffset.UtcNow);
        var ringLength = new RingLength(10);

        // Act
        var cartIndex = tracker.CalculateCartIndexAtOffset(0, ringLength);

        // Assert
        Assert.NotNull(cartIndex);
        Assert.Equal(1, cartIndex.Value.Value);
    }

    [Fact]
    public void CalculateCartIndexAtOffset_WithPositiveOffset_ShouldCalculateCorrectIndex()
    {
        // Arrange
        var tracker = new CartPositionTracker();
        tracker.OnCartPassedOrigin(DateTimeOffset.UtcNow); // Cart 0
        var ringLength = new RingLength(10);

        // Act
        var cartIndex = tracker.CalculateCartIndexAtOffset(5, ringLength);

        // Assert
        Assert.NotNull(cartIndex);
        Assert.Equal(5, cartIndex.Value.Value);
    }

    [Fact]
    public void CalculateCartIndexAtOffset_WithWrapAround_ShouldWrapCorrectly()
    {
        // Arrange
        var tracker = new CartPositionTracker();
        tracker.OnCartPassedOrigin(DateTimeOffset.UtcNow); // Cart 0
        tracker.OnCartPassedOrigin(DateTimeOffset.UtcNow); // Cart 1
        tracker.OnCartPassedOrigin(DateTimeOffset.UtcNow); // Cart 2
        tracker.OnCartPassedOrigin(DateTimeOffset.UtcNow); // Cart 3
        tracker.OnCartPassedOrigin(DateTimeOffset.UtcNow); // Cart 4
        tracker.OnCartPassedOrigin(DateTimeOffset.UtcNow); // Cart 5
        tracker.OnCartPassedOrigin(DateTimeOffset.UtcNow); // Cart 6
        tracker.OnCartPassedOrigin(DateTimeOffset.UtcNow); // Cart 7
        tracker.OnCartPassedOrigin(DateTimeOffset.UtcNow); // Cart 8
        var ringLength = new RingLength(10);

        // Act - offset 5 from cart 8 should be (8 + 5) % 10 = 3
        var cartIndex = tracker.CalculateCartIndexAtOffset(5, ringLength);

        // Assert
        Assert.NotNull(cartIndex);
        Assert.Equal(3, cartIndex.Value.Value); // (8 + 5) % 10 = 13 % 10 = 3
    }

    [Fact]
    public void CalculateCartIndexAtOffset_BeforeFirstCart_ShouldReturnNull()
    {
        // Arrange
        var tracker = new CartPositionTracker();
        var ringLength = new RingLength(10);

        // Act
        var cartIndex = tracker.CalculateCartIndexAtOffset(0, ringLength);

        // Assert
        Assert.Null(cartIndex);
    }
}

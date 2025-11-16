using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Tracking;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Tests.Tracking;

/// <summary>
/// 小车位置跟踪器测试
/// </summary>
public class CartPositionTrackerTests
{
    private static (ICartRingBuilder, ICartPositionTracker) CreateTrackerWithRing(int cartCount)
    {
        var builder = new CartRingBuilder();
        var tracker = new CartPositionTracker(builder);
        
        // Build a cart ring
        var timestamp = DateTimeOffset.UtcNow;
        
        // First zero cart pass - start counting
        builder.OnOriginSensorTriggered(true, true, timestamp);
        builder.OnOriginSensorTriggered(false, true, timestamp.AddMilliseconds(10));
        builder.OnOriginSensorTriggered(true, false, timestamp.AddMilliseconds(50));
        builder.OnOriginSensorTriggered(false, false, timestamp.AddMilliseconds(60));
        
        // Simulate regular carts passing
        for (int i = 1; i < cartCount; i++)
        {
            timestamp = timestamp.AddMilliseconds(100);
            builder.OnOriginSensorTriggered(true, true, timestamp);
            builder.OnOriginSensorTriggered(true, false, timestamp.AddMilliseconds(50));
        }
        
        // Second zero cart pass - complete the ring
        timestamp = timestamp.AddMilliseconds(100);
        builder.OnOriginSensorTriggered(true, true, timestamp);
        builder.OnOriginSensorTriggered(false, true, timestamp.AddMilliseconds(10));
        builder.OnOriginSensorTriggered(true, false, timestamp.AddMilliseconds(50));
        builder.OnOriginSensorTriggered(false, false, timestamp.AddMilliseconds(60));
        
        return (builder, tracker);
    }

    [Fact]
    public void OnCartPassedOrigin_FirstCall_ShouldInitializeTracker()
    {
        // Arrange
        var (builder, tracker) = CreateTrackerWithRing(10);

        // Act
        tracker.OnCartPassedOrigin(DateTimeOffset.UtcNow);

        // Assert
        Assert.True(tracker.IsInitialized);
        Assert.NotNull(tracker.CurrentOriginCartIndex);
        Assert.Equal(0, tracker.CurrentOriginCartIndex.Value.Value);
    }

    [Fact]
    public void OnCartPassedOrigin_MultipleCalls_ShouldIncrementCartIndex()
    {
        // Arrange
        var (builder, tracker) = CreateTrackerWithRing(10);

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
        var (builder, tracker) = CreateTrackerWithRing(10);
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
        var (builder, tracker) = CreateTrackerWithRing(10);
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
        var (builder, tracker) = CreateTrackerWithRing(10);
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
        var builder = new CartRingBuilder();
        var tracker = new CartPositionTracker(builder);
        var ringLength = new RingLength(10);

        // Act
        var cartIndex = tracker.CalculateCartIndexAtOffset(0, ringLength);

        // Assert
        Assert.Null(cartIndex);
    }
    
    [Fact]
    public void OnCartPassedOrigin_WithoutCartRing_ShouldNotInitialize()
    {
        // Arrange
        var builder = new CartRingBuilder();
        var tracker = new CartPositionTracker(builder);

        // Act
        tracker.OnCartPassedOrigin(DateTimeOffset.UtcNow);

        // Assert
        Assert.False(tracker.IsInitialized);
        Assert.Null(tracker.CurrentOriginCartIndex);
    }
}

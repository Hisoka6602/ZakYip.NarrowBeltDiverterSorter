using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Tracking;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Tests.Tracking;

/// <summary>
/// CartRingBuilder测试
/// </summary>
public class CartRingBuilderTests
{
    [Fact]
    public void CartRingBuilder_Should_Build_Snapshot_After_Complete_Ring()
    {
        // Arrange
        var builder = new CartRingBuilder();
        var baseTime = DateTimeOffset.Now;

        // Simulate a complete ring with 5 carts
        // Cart 0 (zero cart) - blocks both sensors
        // Cart 1, 2, 3, 4 - regular carts

        // Act
        // First zero cart - blocks sensor 1
        builder.OnOriginSensorTriggered(true, true, baseTime);
        // First zero cart - blocks sensor 2 (both blocked now)
        builder.OnOriginSensorTriggered(false, true, baseTime.AddMilliseconds(10));
        // First zero cart - unblocks sensor 1
        builder.OnOriginSensorTriggered(true, false, baseTime.AddMilliseconds(20));
        // First zero cart - unblocks sensor 2
        builder.OnOriginSensorTriggered(false, false, baseTime.AddMilliseconds(30));

        // Cart 1 - blocks only sensor 1
        builder.OnOriginSensorTriggered(true, true, baseTime.AddMilliseconds(100));
        builder.OnOriginSensorTriggered(true, false, baseTime.AddMilliseconds(120));

        // Cart 2 - blocks only sensor 1
        builder.OnOriginSensorTriggered(true, true, baseTime.AddMilliseconds(200));
        builder.OnOriginSensorTriggered(true, false, baseTime.AddMilliseconds(220));

        // Cart 3 - blocks only sensor 1
        builder.OnOriginSensorTriggered(true, true, baseTime.AddMilliseconds(300));
        builder.OnOriginSensorTriggered(true, false, baseTime.AddMilliseconds(320));

        // Cart 4 - blocks only sensor 1
        builder.OnOriginSensorTriggered(true, true, baseTime.AddMilliseconds(400));
        builder.OnOriginSensorTriggered(true, false, baseTime.AddMilliseconds(420));

        // Second zero cart - blocks sensor 1
        builder.OnOriginSensorTriggered(true, true, baseTime.AddMilliseconds(500));
        // Second zero cart - blocks sensor 2 (both blocked now, ring complete)
        builder.OnOriginSensorTriggered(false, true, baseTime.AddMilliseconds(510));
        // Second zero cart - unblocks sensor 1
        builder.OnOriginSensorTriggered(true, false, baseTime.AddMilliseconds(520));
        // Second zero cart - unblocks sensor 2
        var completeTime = baseTime.AddMilliseconds(530);
        builder.OnOriginSensorTriggered(false, false, completeTime);

        // Assert
        Assert.NotNull(builder.CurrentSnapshot);
        Assert.Equal(5, builder.CurrentSnapshot!.RingLength.Value);
        Assert.Equal(0, builder.CurrentSnapshot.ZeroCartId.Value);
        Assert.Equal(0, builder.CurrentSnapshot.ZeroIndex.Value);
        Assert.Equal(5, builder.CurrentSnapshot.CartIds.Count);
        Assert.Equal(0, builder.CurrentSnapshot.CartIds[0].Value);
        Assert.Equal(1, builder.CurrentSnapshot.CartIds[1].Value);
        Assert.Equal(2, builder.CurrentSnapshot.CartIds[2].Value);
        Assert.Equal(3, builder.CurrentSnapshot.CartIds[3].Value);
        Assert.Equal(4, builder.CurrentSnapshot.CartIds[4].Value);
    }

    [Fact]
    public void CartRingBuilder_Should_Return_Null_Before_Complete_Ring()
    {
        // Arrange
        var builder = new CartRingBuilder();
        var baseTime = DateTimeOffset.Now;

        // Act
        // Only first zero cart
        builder.OnOriginSensorTriggered(true, true, baseTime);
        builder.OnOriginSensorTriggered(false, true, baseTime.AddMilliseconds(10));
        builder.OnOriginSensorTriggered(true, false, baseTime.AddMilliseconds(20));
        builder.OnOriginSensorTriggered(false, false, baseTime.AddMilliseconds(30));

        // Assert
        Assert.Null(builder.CurrentSnapshot);
    }

    [Fact]
    public void CartRingBuilder_Should_Count_Regular_Carts_Between_Zero_Carts()
    {
        // Arrange
        var builder = new CartRingBuilder();
        var baseTime = DateTimeOffset.Now;

        // Act
        // First zero cart
        builder.OnOriginSensorTriggered(true, true, baseTime);
        builder.OnOriginSensorTriggered(false, true, baseTime.AddMilliseconds(10));
        builder.OnOriginSensorTriggered(true, false, baseTime.AddMilliseconds(20));
        builder.OnOriginSensorTriggered(false, false, baseTime.AddMilliseconds(30));

        // 3 regular carts
        for (int i = 0; i < 3; i++)
        {
            var offset = (i + 1) * 100;
            builder.OnOriginSensorTriggered(true, true, baseTime.AddMilliseconds(offset));
            builder.OnOriginSensorTriggered(true, false, baseTime.AddMilliseconds(offset + 20));
        }

        // Second zero cart
        builder.OnOriginSensorTriggered(true, true, baseTime.AddMilliseconds(500));
        builder.OnOriginSensorTriggered(false, true, baseTime.AddMilliseconds(510));
        builder.OnOriginSensorTriggered(true, false, baseTime.AddMilliseconds(520));
        builder.OnOriginSensorTriggered(false, false, baseTime.AddMilliseconds(530));

        // Assert
        Assert.NotNull(builder.CurrentSnapshot);
        Assert.Equal(4, builder.CurrentSnapshot!.RingLength.Value); // 1 zero cart + 3 regular carts
    }

    [Fact]
    public void CartRingBuilder_Should_Ignore_Events_After_Completion()
    {
        // Arrange
        var builder = new CartRingBuilder();
        var baseTime = DateTimeOffset.Now;

        // Act
        // Complete a ring with just the zero cart going around twice
        builder.OnOriginSensorTriggered(true, true, baseTime);
        builder.OnOriginSensorTriggered(false, true, baseTime.AddMilliseconds(10));
        builder.OnOriginSensorTriggered(true, false, baseTime.AddMilliseconds(20));
        builder.OnOriginSensorTriggered(false, false, baseTime.AddMilliseconds(30));

        builder.OnOriginSensorTriggered(true, true, baseTime.AddMilliseconds(100));
        builder.OnOriginSensorTriggered(false, true, baseTime.AddMilliseconds(110));
        builder.OnOriginSensorTriggered(true, false, baseTime.AddMilliseconds(120));
        builder.OnOriginSensorTriggered(false, false, baseTime.AddMilliseconds(130));

        var snapshot1 = builder.CurrentSnapshot;

        // Try to add more events
        builder.OnOriginSensorTriggered(true, true, baseTime.AddMilliseconds(200));
        builder.OnOriginSensorTriggered(false, true, baseTime.AddMilliseconds(210));

        // Assert
        Assert.NotNull(snapshot1);
        Assert.Same(snapshot1, builder.CurrentSnapshot); // Should be the same snapshot
    }
}

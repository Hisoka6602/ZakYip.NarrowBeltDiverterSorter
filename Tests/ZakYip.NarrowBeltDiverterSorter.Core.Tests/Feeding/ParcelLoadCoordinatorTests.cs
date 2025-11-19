using ZakYip.NarrowBeltDiverterSorter.Core.Application;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Feeding;

#pragma warning disable CS0618 // 此测试类需要测试已标记为过时的事件 ParcelLoadedOnCart，以确保向后兼容性。新代码应使用 IEventBus 订阅事件。

namespace ZakYip.NarrowBeltDiverterSorter.Core.Tests.Feeding;

/// <summary>
/// 包裹装载协调器测试
/// </summary>
public class ParcelLoadCoordinatorTests
{
    [Fact]
    public async Task Coordinator_Should_Create_Parcel_Snapshot_When_Load_Succeeds()
    {
        // Arrange
        var mockPlanner = new MockParcelLoadPlanner(new CartId(5));
        var coordinator = new ParcelLoadCoordinator(mockPlanner);

        ParcelLoadedOnCartEventArgs? capturedEvent = null;
        coordinator.ParcelLoadedOnCart += (sender, e) => capturedEvent = e;

        var parcelId = new ParcelId(100);
        var barcode = "TEST001";
        var infeedTime = DateTimeOffset.UtcNow;

        var eventArgs = new ParcelCreatedFromInfeedEventArgs
        {
            ParcelId = parcelId,
            Barcode = barcode,
            InfeedTriggerTime = infeedTime
        };

        // Act
        coordinator.HandleParcelCreatedFromInfeed(null, eventArgs);
        await Task.Delay(100); // Give async event handler time to complete

        // Assert
        var snapshots = coordinator.GetParcelSnapshots();
        Assert.Single(snapshots);
        Assert.True(snapshots.ContainsKey(parcelId));

        var snapshot = snapshots[parcelId];
        Assert.Equal(parcelId, snapshot.ParcelId);
        Assert.Equal(new CartId(5), snapshot.BoundCartId);
        Assert.Equal(ParcelRouteState.WaitingForRouting, snapshot.RouteState);
        Assert.Equal(infeedTime, snapshot.CreatedAt);
        Assert.NotNull(snapshot.LoadedAt);

        // Check event was published
        Assert.NotNull(capturedEvent);
        Assert.Equal(parcelId, capturedEvent!.ParcelId);
        Assert.Equal(new CartId(5), capturedEvent.CartId);
    }

    [Fact]
    public async Task Coordinator_Should_Create_WaitingForRouting_Snapshot_When_Prediction_Fails()
    {
        // Arrange
        var mockPlanner = new MockParcelLoadPlanner(null); // No cart predicted
        var coordinator = new ParcelLoadCoordinator(mockPlanner);

        ParcelLoadedOnCartEventArgs? capturedEvent = null;
        coordinator.ParcelLoadedOnCart += (sender, e) => capturedEvent = e;

        var parcelId = new ParcelId(101);
        var barcode = "TEST002";
        var infeedTime = DateTimeOffset.UtcNow;

        var eventArgs = new ParcelCreatedFromInfeedEventArgs
        {
            ParcelId = parcelId,
            Barcode = barcode,
            InfeedTriggerTime = infeedTime
        };

        // Act
        coordinator.HandleParcelCreatedFromInfeed(null, eventArgs);
        await Task.Delay(100); // Give async event handler time to complete

        // Assert - 包裹应该保持在等待状态，而不是失败状态
        var snapshots = coordinator.GetParcelSnapshots();
        Assert.Single(snapshots);
        Assert.True(snapshots.ContainsKey(parcelId));

        var snapshot = snapshots[parcelId];
        Assert.Equal(parcelId, snapshot.ParcelId);
        Assert.Null(snapshot.BoundCartId);
        Assert.Equal(ParcelRouteState.WaitingForRouting, snapshot.RouteState); // 保持等待状态

        // Event should not be published when cart ring is not ready
        Assert.Null(capturedEvent);
    }

    [Fact]
    public async Task Coordinator_Should_Handle_Multiple_Parcels()
    {
        // Arrange
        var mockPlanner = new MockParcelLoadPlanner(new CartId(3));
        var coordinator = new ParcelLoadCoordinator(mockPlanner);

        var loadedEvents = new List<ParcelLoadedOnCartEventArgs>();
        coordinator.ParcelLoadedOnCart += (sender, e) => loadedEvents.Add(e);

        // Act
        for (int i = 0; i < 5; i++)
        {
            var eventArgs = new ParcelCreatedFromInfeedEventArgs
            {
                ParcelId = new ParcelId(200 + i),
                Barcode = $"MULTI{i:D3}",
                InfeedTriggerTime = DateTimeOffset.UtcNow.AddSeconds(i)
            };
            coordinator.HandleParcelCreatedFromInfeed(null, eventArgs);
        }

        await Task.Delay(200); // Give async event handlers time to complete

        // Assert
        var snapshots = coordinator.GetParcelSnapshots();
        Assert.Equal(5, snapshots.Count);
        Assert.Equal(5, loadedEvents.Count);

        for (int i = 0; i < 5; i++)
        {
            var parcelId = new ParcelId(200 + i);
            Assert.True(snapshots.ContainsKey(parcelId));
            Assert.Equal(new CartId(3), snapshots[parcelId].BoundCartId);
        }
    }
}

/// <summary>
/// 模拟包裹装载计划器
/// </summary>
internal class MockParcelLoadPlanner : IParcelLoadPlanner
{
    private readonly CartId? _predictedCart;

    public MockParcelLoadPlanner(CartId? predictedCart)
    {
        _predictedCart = predictedCart;
    }

    public Task<CartId?> PredictLoadedCartAsync(DateTimeOffset infeedEdgeTime, CancellationToken ct)
    {
        return Task.FromResult(_predictedCart);
    }
}

using ZakYip.NarrowBeltDiverterSorter.Core.Application;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Parcels;
using ZakYip.NarrowBeltDiverterSorter.Core.Tests.Fakes;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Tests.Parcels;

/// <summary>
/// 包裹生命周期服务测试
/// </summary>
public class ParcelLifecycleServiceTests
{
    private static ParcelLifecycleService CreateServiceInRunningState()
    {
        var systemRunStateService = new FakeSystemRunStateService();
        systemRunStateService.SetState(SystemRunState.Running);
        return new ParcelLifecycleService(systemRunStateService);
    }

    [Fact]
    public void CreateParcel_Should_Create_New_Parcel_With_WaitingForRouting_State()
    {
        // Arrange
        var service = CreateServiceInRunningState();
        var parcelId = new ParcelId(1234567890123);
        var barcode = "TEST001";
        var infeedTime = DateTimeOffset.Now;

        // Act
        var parcel = service.CreateParcel(parcelId, barcode, infeedTime);

        // Assert
        Assert.NotNull(parcel);
        Assert.Equal(parcelId, parcel.ParcelId);
        Assert.Equal(ParcelRouteState.WaitingForRouting, parcel.RouteState);
        Assert.Equal(infeedTime, parcel.CreatedAt);
        Assert.Null(parcel.TargetChuteId);
        Assert.Null(parcel.BoundCartId);
    }

    [Fact]
    public void CreateParcel_Should_Throw_When_Parcel_Already_Exists()
    {
        // Arrange
        var service = CreateServiceInRunningState();
        var parcelId = new ParcelId(1234567890123);
        var barcode = "TEST001";
        var infeedTime = DateTimeOffset.Now;

        service.CreateParcel(parcelId, barcode, infeedTime);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            service.CreateParcel(parcelId, barcode, infeedTime));
    }

    [Fact]
    public void BindChuteId_Should_Set_ChuteId_And_Update_State_To_Routed()
    {
        // Arrange
        var service = CreateServiceInRunningState();
        var parcelId = new ParcelId(1234567890123);
        var chuteId = new ChuteId(5);

        service.CreateParcel(parcelId, "TEST001", DateTimeOffset.Now);

        // Act
        service.BindChuteId(parcelId, chuteId);

        // Assert
        var parcel = service.Get(parcelId);
        Assert.NotNull(parcel);
        Assert.Equal(chuteId, parcel.TargetChuteId);
        Assert.Equal(ParcelRouteState.Routed, parcel.RouteState);
    }

    [Fact]
    public void BindChuteId_Should_Throw_When_Parcel_Not_Exists()
    {
        // Arrange
        var service = CreateServiceInRunningState();
        var parcelId = new ParcelId(1234567890123);
        var chuteId = new ChuteId(5);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            service.BindChuteId(parcelId, chuteId));
    }

    [Fact]
    public void BindCartId_Should_Set_CartId_And_Update_State_To_Sorting()
    {
        // Arrange
        var service = CreateServiceInRunningState();
        var parcelId = new ParcelId(1234567890123);
        var chuteId = new ChuteId(5);
        var cartId = new CartId(10);
        var loadedTime = DateTimeOffset.Now;

        service.CreateParcel(parcelId, "TEST001", DateTimeOffset.Now);
        service.BindChuteId(parcelId, chuteId);

        // Act
        service.BindCartId(parcelId, cartId, loadedTime);

        // Assert
        var parcel = service.Get(parcelId);
        Assert.NotNull(parcel);
        Assert.Equal(cartId, parcel.BoundCartId);
        Assert.Equal(loadedTime, parcel.LoadedAt);
        Assert.Equal(ParcelRouteState.Sorting, parcel.RouteState);
    }

    [Fact]
    public void UnbindCartId_Should_Clear_CartId()
    {
        // Arrange
        var service = CreateServiceInRunningState();
        var parcelId = new ParcelId(1234567890123);
        var chuteId = new ChuteId(5);
        var cartId = new CartId(10);
        var loadedTime = DateTimeOffset.Now;

        service.CreateParcel(parcelId, "TEST001", DateTimeOffset.Now);
        service.BindChuteId(parcelId, chuteId);
        service.BindCartId(parcelId, cartId, loadedTime);

        // Act
        service.UnbindCartId(parcelId);

        // Assert
        var parcel = service.Get(parcelId);
        Assert.NotNull(parcel);
        Assert.Null(parcel.BoundCartId);
    }

    [Fact]
    public void UpdateRouteState_Should_Update_State()
    {
        // Arrange
        var service = CreateServiceInRunningState();
        var parcelId = new ParcelId(1234567890123);

        service.CreateParcel(parcelId, "TEST001", DateTimeOffset.Now);

        // Act
        service.UpdateRouteState(parcelId, ParcelRouteState.Failed);

        // Assert
        var parcel = service.Get(parcelId);
        Assert.NotNull(parcel);
        Assert.Equal(ParcelRouteState.Failed, parcel.RouteState);
    }

    [Fact]
    public void MarkSorted_Should_Set_SortedAt_And_Update_State()
    {
        // Arrange
        var service = CreateServiceInRunningState();
        var parcelId = new ParcelId(1234567890123);
        var chuteId = new ChuteId(5);
        var cartId = new CartId(10);
        var sortedTime = DateTimeOffset.Now;

        service.CreateParcel(parcelId, "TEST001", DateTimeOffset.Now);
        service.BindChuteId(parcelId, chuteId);
        service.BindCartId(parcelId, cartId, DateTimeOffset.Now);

        // Act
        service.MarkSorted(parcelId, sortedTime);

        // Assert
        var parcel = service.Get(parcelId);
        Assert.NotNull(parcel);
        Assert.Equal(ParcelRouteState.Sorted, parcel.RouteState);
        Assert.Equal(sortedTime, parcel.SortedAt);
    }

    [Fact]
    public void Get_Should_Return_Null_When_Parcel_Not_Exists()
    {
        // Arrange
        var service = CreateServiceInRunningState();
        var parcelId = new ParcelId(1234567890123);

        // Act
        var parcel = service.Get(parcelId);

        // Assert
        Assert.Null(parcel);
    }

    [Fact]
    public void Get_Should_Return_Parcel_When_Exists()
    {
        // Arrange
        var service = CreateServiceInRunningState();
        var parcelId = new ParcelId(1234567890123);
        var barcode = "TEST001";
        var infeedTime = DateTimeOffset.Now;

        service.CreateParcel(parcelId, barcode, infeedTime);

        // Act
        var parcel = service.Get(parcelId);

        // Assert
        Assert.NotNull(parcel);
        Assert.Equal(parcelId, parcel.ParcelId);
    }
}

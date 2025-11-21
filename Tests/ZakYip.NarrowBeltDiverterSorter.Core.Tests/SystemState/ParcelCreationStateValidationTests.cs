using ZakYip.NarrowBeltDiverterSorter.Core.Application;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.SystemState;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Tests.SystemState;

/// <summary>
/// 包裹创建状态验证测试
/// </summary>
public class ParcelCreationStateValidationTests
{
    [Fact]
    public void CreateParcel_When_System_Running_Should_Succeed()
    {
        // Arrange
        var systemRunStateService = new SystemRunStateService();
        systemRunStateService.TryHandleStart();
        var service = new ParcelLifecycleService(systemRunStateService);
        
        var parcelId = new ParcelId(1234567890123);
        var barcode = "TEST001";
        var infeedTime = DateTimeOffset.Now;

        // Act
        var parcel = service.CreateParcel(parcelId, barcode, infeedTime);

        // Assert
        Assert.NotNull(parcel);
        Assert.Equal(parcelId, parcel.ParcelId);
    }

    [Fact]
    public void CreateParcel_When_System_Stopped_Initial_Should_Throw()
    {
        // Arrange
        var systemRunStateService = new SystemRunStateService();
        Assert.Equal(SystemRunState.Stopped, systemRunStateService.Current);
        var service = new ParcelLifecycleService(systemRunStateService);
        
        var parcelId = new ParcelId(1234567890123);
        var barcode = "TEST001";
        var infeedTime = DateTimeOffset.Now;

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            service.CreateParcel(parcelId, barcode, infeedTime));
        
        Assert.Contains("停止", exception.Message);
        Assert.Contains("禁止创建包裹", exception.Message);
    }

    [Fact]
    public void CreateParcel_When_System_Stopped_After_Stop_Should_Throw()
    {
        // Arrange
        var systemRunStateService = new SystemRunStateService();
        systemRunStateService.TryHandleStart(); // Start first
        systemRunStateService.TryHandleStop(); // Then stop
        Assert.Equal(SystemRunState.Stopped, systemRunStateService.Current);
        var service = new ParcelLifecycleService(systemRunStateService);
        
        var parcelId = new ParcelId(1234567890123);
        var barcode = "TEST001";
        var infeedTime = DateTimeOffset.Now;

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            service.CreateParcel(parcelId, barcode, infeedTime));
        
        Assert.Contains("停止", exception.Message);
        Assert.Contains("禁止创建包裹", exception.Message);
    }

    [Fact]
    public void CreateParcel_When_System_Fault_Should_Throw()
    {
        // Arrange
        var systemRunStateService = new SystemRunStateService();
        systemRunStateService.TryHandleEmergencyStop();
        Assert.Equal(SystemRunState.Fault, systemRunStateService.Current);
        var service = new ParcelLifecycleService(systemRunStateService);
        
        var parcelId = new ParcelId(1234567890123);
        var barcode = "TEST001";
        var infeedTime = DateTimeOffset.Now;

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            service.CreateParcel(parcelId, barcode, infeedTime));
        
        Assert.Contains("故障状态", exception.Message);
        Assert.Contains("禁止创建包裹", exception.Message);
    }

    [Fact]
    public void CreateParcel_State_Transition_Should_Affect_Creation()
    {
        // Arrange
        var systemRunStateService = new SystemRunStateService();
        var service = new ParcelLifecycleService(systemRunStateService);
        
        var parcelId1 = new ParcelId(1000000000001);
        var parcelId2 = new ParcelId(1000000000002);
        var barcode = "TEST";
        var infeedTime = DateTimeOffset.Now;

        // Act & Assert: Stopped state (initial) - should fail
        Assert.Equal(SystemRunState.Stopped, systemRunStateService.Current);
        Assert.Throws<InvalidOperationException>(() =>
            service.CreateParcel(parcelId1, barcode, infeedTime));

        // Transition to Running
        systemRunStateService.TryHandleStart();
        Assert.Equal(SystemRunState.Running, systemRunStateService.Current);
        
        // Should succeed now
        var parcel = service.CreateParcel(parcelId2, barcode, infeedTime);
        Assert.NotNull(parcel);
    }
}

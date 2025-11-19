using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.SystemState;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using ZakYip.NarrowBeltDiverterSorter.Observability.LiveView;

namespace ZakYip.NarrowBeltDiverterSorter.Observability.Tests.LiveView;

/// <summary>
/// NarrowBeltLiveView 测试
/// </summary>
public class NarrowBeltLiveViewTests
{
    private readonly IEventBus _eventBus;
    private readonly ISystemFaultService _faultService;
    private readonly NarrowBeltLiveView _liveView;

    public NarrowBeltLiveViewTests()
    {
        _eventBus = new InMemoryEventBus(NullLogger<InMemoryEventBus>.Instance);
        _faultService = new SystemFaultService();
        _liveView = new NarrowBeltLiveView(_eventBus, _faultService, NullLogger<NarrowBeltLiveView>.Instance);
    }

    [Fact]
    public async Task LineSpeedChangedEvent_UpdatesLineSpeedSnapshot()
    {
        // Arrange
        var eventArgs = new LineSpeedChangedEventArgs
        {
            ActualMmps = 100.5m,
            TargetMmps = 120.0m,
            Status = LineSpeedStatus.Starting,
            OccurredAt = DateTimeOffset.UtcNow
        };

        // Act
        await _eventBus.PublishAsync(eventArgs);
        await Task.Delay(100); // 等待事件处理

        // Assert
        var snapshot = _liveView.GetLineSpeed();
        Assert.Equal(100.5m, snapshot.ActualMmps);
        Assert.Equal(120.0m, snapshot.TargetMmps);
        Assert.Equal(LineSpeedStatus.Starting, snapshot.Status);
    }

    [Fact]
    public async Task ParcelCreatedEvent_UpdatesLastCreatedParcel()
    {
        // Arrange
        var eventArgs = new ParcelCreatedEventArgs
        {
            ParcelId = 12345,
            Barcode = "TEST-BARCODE-001",
            WeightKg = 2.5m,
            VolumeCubicMm = 1000000m,
            TargetChuteId = 10,
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Act
        await _eventBus.PublishAsync(eventArgs);
        await Task.Delay(100); // 等待事件处理

        // Assert
        var lastCreated = _liveView.GetLastCreatedParcel();
        Assert.NotNull(lastCreated);
        Assert.Equal(12345, lastCreated.ParcelId);
        Assert.Equal("TEST-BARCODE-001", lastCreated.Barcode);
        Assert.Equal(2.5m, lastCreated.WeightKg);
        Assert.Equal(10, lastCreated.TargetChuteId);

        // 验证包裹在在线列表中
        var onlineParcels = _liveView.GetOnlineParcels();
        Assert.Single(onlineParcels);
        Assert.Contains(onlineParcels, p => p.ParcelId == 12345);
    }

    [Fact]
    public async Task ParcelDivertedEvent_UpdatesLastDivertedParcelAndRemovesFromOnline()
    {
        // Arrange - 先创建包裹
        var createEvent = new ParcelCreatedEventArgs
        {
            ParcelId = 12345,
            Barcode = "TEST-BARCODE-001",
            WeightKg = 2.5m,
            VolumeCubicMm = 1000000m,
            TargetChuteId = 10,
            CreatedAt = DateTimeOffset.UtcNow
        };
        await _eventBus.PublishAsync(createEvent);
        await Task.Delay(100);

        var divertEvent = new ParcelDivertedEventArgs
        {
            ParcelId = 12345,
            Barcode = "TEST-BARCODE-001",
            WeightKg = 2.5m,
            VolumeCubicMm = 1000000m,
            TargetChuteId = 10,
            ActualChuteId = 10,
            DivertedAt = DateTimeOffset.UtcNow
        };

        // Act
        await _eventBus.PublishAsync(divertEvent);
        await Task.Delay(100); // 等待事件处理

        // Assert
        var lastDiverted = _liveView.GetLastDivertedParcel();
        Assert.NotNull(lastDiverted);
        Assert.Equal(12345, lastDiverted.ParcelId);
        Assert.Equal(10, lastDiverted.ActualChuteId);

        // 验证包裹不再在在线列表中
        var onlineParcels = _liveView.GetOnlineParcels();
        Assert.Empty(onlineParcels);
    }

    [Fact]
    public async Task OriginCartChangedEvent_UpdatesOriginCartSnapshot()
    {
        // Arrange
        var eventArgs = new OriginCartChangedEventArgs
        {
            CartId = 5,
            OccurredAt = DateTimeOffset.UtcNow
        };

        // Act
        await _eventBus.PublishAsync(eventArgs);
        await Task.Delay(100); // 等待事件处理

        // Assert
        var snapshot = _liveView.GetOriginCart();
        Assert.Equal(5, snapshot.CartId);
    }

    [Fact]
    public async Task CartAtChuteChangedEvent_UpdatesChuteCartMapping()
    {
        // Arrange
        var eventArgs = new CartAtChuteChangedEventArgs
        {
            ChuteId = 10,
            CartId = 5,
            OccurredAt = DateTimeOffset.UtcNow
        };

        // Act
        await _eventBus.PublishAsync(eventArgs);
        await Task.Delay(100); // 等待事件处理

        // Assert
        var cartId = _liveView.GetChuteCart(10);
        Assert.Equal(5, cartId);

        var chuteCartsSnapshot = _liveView.GetChuteCarts();
        Assert.True(chuteCartsSnapshot.Mapping.ContainsKey(10));
        Assert.Equal(5, chuteCartsSnapshot.Mapping[10]);
    }

    [Fact]
    public async Task DeviceStatusChangedEvent_UpdatesDeviceStatusSnapshot()
    {
        // Arrange
        var eventArgs = new DeviceStatusChangedEventArgs
        {
            Status = DeviceStatus.Running,
            Message = "系统正常运行",
            OccurredAt = DateTimeOffset.UtcNow
        };

        // Act
        await _eventBus.PublishAsync(eventArgs);
        await Task.Delay(100); // 等待事件处理

        // Assert
        var snapshot = _liveView.GetDeviceStatus();
        Assert.Equal(DeviceStatus.Running, snapshot.Status);
        Assert.Equal("系统正常运行", snapshot.Message);
    }

    [Fact]
    public async Task CartLayoutChangedEvent_UpdatesCartLayoutSnapshot()
    {
        // Arrange
        var cartPositions = new List<CartPositionSnapshot>
        {
            new() { CartId = 1, CartIndex = 0, LinearPositionMm = 100m, CurrentChuteId = null },
            new() { CartId = 2, CartIndex = 1, LinearPositionMm = 200m, CurrentChuteId = 5 }
        };

        var chuteMapping = new Dictionary<long, long?>
        {
            { 5, 2 },
            { 6, null }
        };

        var eventArgs = new CartLayoutChangedEventArgs
        {
            CartPositions = cartPositions,
            ChuteToCartMapping = chuteMapping,
            OccurredAt = DateTimeOffset.UtcNow
        };

        // Act
        await _eventBus.PublishAsync(eventArgs);
        await Task.Delay(100); // 等待事件处理

        // Assert
        var layoutSnapshot = _liveView.GetCartLayout();
        Assert.Equal(2, layoutSnapshot.CartPositions.Count);
        Assert.Contains(layoutSnapshot.CartPositions, cp => cp.CartId == 1);
        Assert.Contains(layoutSnapshot.CartPositions, cp => cp.CartId == 2);

        var chuteCartsSnapshot = _liveView.GetChuteCarts();
        Assert.Equal(2, chuteMapping[5]);
        Assert.Null(chuteMapping[6]);
    }

    [Fact]
    public void GetInitialState_ReturnsDefaultValues()
    {
        // Assert
        var lineSpeed = _liveView.GetLineSpeed();
        Assert.Equal(0m, lineSpeed.ActualMmps);
        Assert.Equal(LineSpeedStatus.Unknown, lineSpeed.Status);

        var originCart = _liveView.GetOriginCart();
        Assert.Null(originCart.CartId);

        var onlineParcels = _liveView.GetOnlineParcels();
        Assert.Empty(onlineParcels);

        var deviceStatus = _liveView.GetDeviceStatus();
        Assert.Equal(DeviceStatus.Idle, deviceStatus.Status);

        var cartLayout = _liveView.GetCartLayout();
        Assert.Empty(cartLayout.CartPositions);
    }
}

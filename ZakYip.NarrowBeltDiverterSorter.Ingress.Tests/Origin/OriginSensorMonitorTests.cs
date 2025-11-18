using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Tracking;
using ZakYip.NarrowBeltDiverterSorter.Ingress.Origin;
using ZakYip.NarrowBeltDiverterSorter.Observability;
using Microsoft.Extensions.Logging.Abstractions;

namespace ZakYip.NarrowBeltDiverterSorter.Ingress.Tests.Origin;

/// <summary>
/// OriginSensorMonitor测试
/// </summary>
public class OriginSensorMonitorTests
{
    /// <summary>
    /// Mock事件总线（仅用于测试）
    /// </summary>
    private class MockEventBus : IEventBus
    {
        public void Subscribe<TEventArgs>(Func<TEventArgs, CancellationToken, Task> handler) where TEventArgs : class { }
        public void Unsubscribe<TEventArgs>(Func<TEventArgs, CancellationToken, Task> handler) where TEventArgs : class { }
        public Task PublishAsync<TEventArgs>(TEventArgs eventArgs, CancellationToken cancellationToken = default) where TEventArgs : class => Task.CompletedTask;
        public int GetBacklogCount() => 0;
    }

    /// <summary>
    /// Mock原点传感器端口
    /// </summary>
    private class MockOriginSensorPort : IOriginSensorPort
    {
        private bool _sensor1State = false;
        private bool _sensor2State = false;

        public void SetSensor1State(bool state) => _sensor1State = state;
        public void SetSensor2State(bool state) => _sensor2State = state;

        public bool GetFirstSensorState() => _sensor1State;
        public bool GetSecondSensorState() => _sensor2State;
    }

    [Fact]
    public async Task OriginSensorMonitor_Should_Detect_Cart_Ring_With_Virtual_IO_Sequence()
    {
        // Arrange
        var mockPort = new MockOriginSensorPort();
        var builder = new CartRingBuilder();
        var tracker = new CartPositionTracker(builder);
        var monitor = new OriginSensorMonitor(
            mockPort,
            builder,
            tracker,
            new MockEventBus(),
            NullLogger<OriginSensorMonitor>.Instance,
            TimeSpan.FromMilliseconds(5));

        // Start monitoring
        await monitor.StartAsync();

        // Act - Simulate a complete ring with 5 carts
        // Cart 0 (zero cart) - blocks both sensors
        mockPort.SetSensor1State(true);
        await Task.Delay(10);
        mockPort.SetSensor2State(true);
        await Task.Delay(10);
        mockPort.SetSensor1State(false);
        await Task.Delay(10);
        mockPort.SetSensor2State(false);
        await Task.Delay(20);

        // Cart 1 - blocks only sensor 1
        mockPort.SetSensor1State(true);
        await Task.Delay(10);
        mockPort.SetSensor1State(false);
        await Task.Delay(20);

        // Cart 2 - blocks only sensor 1
        mockPort.SetSensor1State(true);
        await Task.Delay(10);
        mockPort.SetSensor1State(false);
        await Task.Delay(20);

        // Cart 3 - blocks only sensor 1
        mockPort.SetSensor1State(true);
        await Task.Delay(10);
        mockPort.SetSensor1State(false);
        await Task.Delay(20);

        // Cart 4 - blocks only sensor 1
        mockPort.SetSensor1State(true);
        await Task.Delay(10);
        mockPort.SetSensor1State(false);
        await Task.Delay(20);

        // Cart 0 again (zero cart) - blocks both sensors (completes ring)
        mockPort.SetSensor1State(true);
        await Task.Delay(10);
        mockPort.SetSensor2State(true);
        await Task.Delay(10);
        mockPort.SetSensor1State(false);
        await Task.Delay(10);
        mockPort.SetSensor2State(false);
        await Task.Delay(20);

        // Stop monitoring
        await monitor.StopAsync();

        // Assert
        Assert.NotNull(builder.CurrentSnapshot);
        Assert.Equal(5, builder.CurrentSnapshot!.RingLength.Value);
        Assert.Equal(0, builder.CurrentSnapshot.ZeroCartId.Value);
        Assert.Equal(5, builder.CurrentSnapshot.CartIds.Count);
    }

    [Fact]
    public async Task OriginSensorMonitor_Should_Detect_Edges_Correctly()
    {
        // Arrange
        var mockPort = new MockOriginSensorPort();
        var builder = new CartRingBuilder();
        var tracker = new CartPositionTracker(builder);
        var monitor = new OriginSensorMonitor(
            mockPort,
            builder,
            tracker,
            new MockEventBus(),
            NullLogger<OriginSensorMonitor>.Instance,
            TimeSpan.FromMilliseconds(5));

        // Start monitoring
        await monitor.StartAsync();

        // Act - Simulate a simple edge detection
        mockPort.SetSensor1State(true); // Rising edge on sensor 1
        await Task.Delay(20);
        mockPort.SetSensor1State(false); // Falling edge on sensor 1
        await Task.Delay(20);

        // Stop monitoring
        await monitor.StopAsync();

        // Assert - No snapshot should be created (no complete ring)
        Assert.Null(builder.CurrentSnapshot);
    }

    [Fact]
    public async Task OriginSensorMonitor_Should_Stop_Gracefully()
    {
        // Arrange
        var mockPort = new MockOriginSensorPort();
        var builder = new CartRingBuilder();
        var tracker = new CartPositionTracker(builder);
        var monitor = new OriginSensorMonitor(
            mockPort,
            builder,
            tracker,
            new MockEventBus(),
            NullLogger<OriginSensorMonitor>.Instance,
            TimeSpan.FromMilliseconds(5));

        // Act
        await monitor.StartAsync();
        await Task.Delay(50);
        await monitor.StopAsync();

        // Assert - Should complete without exception
        Assert.True(true);
    }

    [Fact]
    public void OriginSensorMonitor_Should_Throw_ArgumentNullException_For_Null_SensorPort()
    {
        // Arrange & Act & Assert
        var builder = new CartRingBuilder();
        Assert.Throws<ArgumentNullException>(() => 
            new OriginSensorMonitor(null!, builder, new CartPositionTracker(builder), new MockEventBus(), NullLogger<OriginSensorMonitor>.Instance));
    }

    [Fact]
    public void OriginSensorMonitor_Should_Throw_ArgumentNullException_For_Null_Builder()
    {
        // Arrange & Act & Assert
        var mockPort = new MockOriginSensorPort();
        var builder = new CartRingBuilder();
        Assert.Throws<ArgumentNullException>(() => 
            new OriginSensorMonitor(mockPort, null!, new CartPositionTracker(builder), new MockEventBus(), NullLogger<OriginSensorMonitor>.Instance));
    }

    [Fact]
    public async Task OriginSensorMonitor_Should_Handle_Multiple_Start_Calls()
    {
        // Arrange
        var mockPort = new MockOriginSensorPort();
        var builder = new CartRingBuilder();
        var tracker = new CartPositionTracker(builder);
        var monitor = new OriginSensorMonitor(
            mockPort,
            builder,
            tracker,
            new MockEventBus(),
            NullLogger<OriginSensorMonitor>.Instance,
            TimeSpan.FromMilliseconds(5));

        // Act
        await monitor.StartAsync();
        await monitor.StartAsync(); // Should be ignored
        await Task.Delay(50);
        await monitor.StopAsync();

        // Assert - Should complete without exception
        Assert.True(true);
    }
}

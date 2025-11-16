using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Feeding;
using ZakYip.NarrowBeltDiverterSorter.Ingress.Infeed;

namespace ZakYip.NarrowBeltDiverterSorter.Ingress.Tests.Infeed;

/// <summary>
/// 入口传感器监视器测试
/// </summary>
public class InfeedSensorMonitorTests
{
    [Fact]
    public async Task Monitor_Should_Generate_ParcelId_On_Rising_Edge()
    {
        // Arrange
        var mockSensorPort = new MockInfeedSensorPort();
        var monitor = new InfeedSensorMonitor(mockSensorPort);

        ParcelCreatedFromInfeedEventArgs? capturedEvent = null;
        monitor.ParcelCreatedFromInfeed += (sender, e) => capturedEvent = e;

        await monitor.StartAsync();

        // Act
        var detectionTime = DateTimeOffset.UtcNow;
        mockSensorPort.TriggerParcelDetection(true, detectionTime);

        await Task.Delay(100); // Give event time to propagate

        // Assert
        Assert.NotNull(capturedEvent);
        Assert.Equal(new ParcelId(1), capturedEvent!.ParcelId);
        Assert.Equal("PARCEL0000000001", capturedEvent.Barcode);
        Assert.Equal(detectionTime, capturedEvent.InfeedTriggerTime);

        await monitor.StopAsync();
    }

    [Fact]
    public async Task Monitor_Should_Ignore_Falling_Edge()
    {
        // Arrange
        var mockSensorPort = new MockInfeedSensorPort();
        var monitor = new InfeedSensorMonitor(mockSensorPort);

        ParcelCreatedFromInfeedEventArgs? capturedEvent = null;
        monitor.ParcelCreatedFromInfeed += (sender, e) => capturedEvent = e;

        await monitor.StartAsync();

        // Act
        mockSensorPort.TriggerParcelDetection(false, DateTimeOffset.UtcNow); // Falling edge

        await Task.Delay(100);

        // Assert
        Assert.Null(capturedEvent); // No event should be generated

        await monitor.StopAsync();
    }

    [Fact]
    public async Task Monitor_Should_Generate_Sequential_ParcelIds()
    {
        // Arrange
        var mockSensorPort = new MockInfeedSensorPort();
        var monitor = new InfeedSensorMonitor(mockSensorPort);

        var capturedEvents = new List<ParcelCreatedFromInfeedEventArgs>();
        monitor.ParcelCreatedFromInfeed += (sender, e) => capturedEvents.Add(e);

        await monitor.StartAsync();

        // Act
        for (int i = 0; i < 5; i++)
        {
            mockSensorPort.TriggerParcelDetection(true, DateTimeOffset.UtcNow.AddSeconds(i));
            await Task.Delay(50);
        }

        await Task.Delay(100);

        // Assert
        Assert.Equal(5, capturedEvents.Count);
        for (int i = 0; i < 5; i++)
        {
            Assert.Equal(new ParcelId(i + 1), capturedEvents[i].ParcelId);
            Assert.Equal($"PARCEL{(i + 1):D10}", capturedEvents[i].Barcode);
        }

        await monitor.StopAsync();
    }

    [Fact]
    public async Task Monitor_Should_Handle_Rapid_Detections()
    {
        // Arrange
        var mockSensorPort = new MockInfeedSensorPort();
        var monitor = new InfeedSensorMonitor(mockSensorPort);

        var capturedEvents = new List<ParcelCreatedFromInfeedEventArgs>();
        monitor.ParcelCreatedFromInfeed += (sender, e) => capturedEvents.Add(e);

        await monitor.StartAsync();

        // Act - Simulate rapid detections
        var baseTime = DateTimeOffset.UtcNow;
        for (int i = 0; i < 10; i++)
        {
            mockSensorPort.TriggerParcelDetection(true, baseTime.AddMilliseconds(i * 10));
        }

        await Task.Delay(200);

        // Assert
        Assert.Equal(10, capturedEvents.Count);
        for (int i = 0; i < 10; i++)
        {
            Assert.Equal(new ParcelId(i + 1), capturedEvents[i].ParcelId);
        }

        await monitor.StopAsync();
    }
}

/// <summary>
/// 模拟入口传感器端口
/// </summary>
internal class MockInfeedSensorPort : IInfeedSensorPort
{
    private bool _currentState;

    public event EventHandler<ParcelDetectedEventArgs>? ParcelDetected;

    public bool GetCurrentState() => _currentState;

    public Task StartMonitoringAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task StopMonitoringAsync()
    {
        return Task.CompletedTask;
    }

    public void TriggerParcelDetection(bool isBlocked, DateTimeOffset detectionTime)
    {
        _currentState = isBlocked;
        ParcelDetected?.Invoke(this, new ParcelDetectedEventArgs
        {
            DetectionTime = detectionTime,
            IsBlocked = isBlocked
        });
    }
}

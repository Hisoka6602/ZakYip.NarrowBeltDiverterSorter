using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;

namespace ZakYip.NarrowBeltDiverterSorter.Simulation.Fakes;

/// <summary>
/// 模拟入口传感器端口
/// </summary>
public class FakeInfeedSensorPort : IInfeedSensorPort
{
    private CancellationTokenSource? _monitoringCts;

    public event EventHandler<ParcelDetectedEventArgs>? ParcelDetected;

    public bool GetCurrentState()
    {
        return false;
    }

    public Task StartMonitoringAsync(CancellationToken cancellationToken = default)
    {
        _monitoringCts = new CancellationTokenSource();
        Console.WriteLine($"[入口传感器] 开始监听");
        return Task.CompletedTask;
    }

    public Task StopMonitoringAsync()
    {
        _monitoringCts?.Cancel();
        Console.WriteLine($"[入口传感器] 停止监听");
        return Task.CompletedTask;
    }

    public void SimulateParcelDetection()
    {
        var eventArgs = new ParcelDetectedEventArgs
        {
            DetectionTime = DateTimeOffset.Now,
            IsBlocked = true
        };
        Console.WriteLine($"[入口传感器] 检测到包裹 - {eventArgs.DetectionTime:HH:mm:ss.fff}");
        ParcelDetected?.Invoke(this, eventArgs);
    }
}

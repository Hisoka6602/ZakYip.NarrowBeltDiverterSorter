using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Feeding;

namespace ZakYip.NarrowBeltDiverterSorter.Ingress.Infeed;

/// <summary>
/// 入口传感器监视器
/// 监听入口IO的上升沿/有效进入信号
/// </summary>
public class InfeedSensorMonitor
{
    private readonly IInfeedSensorPort _sensorPort;
    private long _nextParcelIdCounter = 1;

    /// <summary>
    /// 包裹从入口创建事件
    /// </summary>
    public event EventHandler<ParcelCreatedFromInfeedEventArgs>? ParcelCreatedFromInfeed;

    /// <summary>
    /// 创建入口传感器监视器
    /// </summary>
    /// <param name="sensorPort">传感器端口</param>
    public InfeedSensorMonitor(IInfeedSensorPort sensorPort)
    {
        _sensorPort = sensorPort ?? throw new ArgumentNullException(nameof(sensorPort));
        _sensorPort.ParcelDetected += OnParcelDetected;
    }

    /// <summary>
    /// 启动监控
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await _sensorPort.StartMonitoringAsync(cancellationToken);
    }

    /// <summary>
    /// 停止监控
    /// </summary>
    public async Task StopAsync()
    {
        await _sensorPort.StopMonitoringAsync();
    }

    private void OnParcelDetected(object? sender, ParcelDetectedEventArgs e)
    {
        // 只处理上升沿（遮挡信号）
        if (!e.IsBlocked)
        {
            return;
        }

        // 生成新的包裹ID
        var parcelId = new ParcelId(_nextParcelIdCounter++);

        // 生成条码（这里使用简单的格式，实际应用中可能需要从其他源获取）
        var barcode = $"PARCEL{parcelId.Value:D10}";

        // 发布事件
        ParcelCreatedFromInfeed?.Invoke(this, new ParcelCreatedFromInfeedEventArgs
        {
            ParcelId = parcelId,
            Barcode = barcode,
            InfeedTriggerTime = e.DetectionTime
        });
    }
}

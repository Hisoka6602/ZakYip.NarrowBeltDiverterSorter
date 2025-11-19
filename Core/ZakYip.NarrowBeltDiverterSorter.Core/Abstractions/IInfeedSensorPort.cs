namespace ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;

/// <summary>
/// 入口传感器端口接口
/// 对应物理连接：入口光电传感器（检测包裹进入）
/// </summary>
public interface IInfeedSensorPort
{
    /// <summary>
    /// 包裹检测事件
    /// 当传感器检测到包裹进入时触发
    /// </summary>
    event EventHandler<ParcelDetectedEventArgs>? ParcelDetected;

    /// <summary>
    /// 获取当前传感器状态
    /// </summary>
    /// <returns>true表示有物体遮挡，false表示无遮挡</returns>
    bool GetCurrentState();

    /// <summary>
    /// 启动传感器监听
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    Task StartMonitoringAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 停止传感器监听
    /// </summary>
    /// <returns>异步任务</returns>
    Task StopMonitoringAsync();
}

/// <summary>
/// 包裹检测事件参数
/// </summary>
public class ParcelDetectedEventArgs : EventArgs
{
    /// <summary>
    /// 检测时间
    /// </summary>
    public required DateTimeOffset DetectionTime { get; init; }

    /// <summary>
    /// 传感器状态（true=遮挡，false=无遮挡）
    /// </summary>
    public required bool IsBlocked { get; init; }
}

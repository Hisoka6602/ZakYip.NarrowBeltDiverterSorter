namespace ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;

/// <summary>
/// IO监视器接口
/// 定义IO监控的生命周期管理（启动、停止）
/// </summary>
public interface IIoMonitor
{
    /// <summary>
    /// 启动监控
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 停止监控
    /// </summary>
    /// <returns>异步任务</returns>
    Task StopAsync();

    /// <summary>
    /// 获取监控是否正在运行
    /// </summary>
    /// <returns>true表示正在运行，false表示已停止</returns>
    bool IsRunning { get; }
}

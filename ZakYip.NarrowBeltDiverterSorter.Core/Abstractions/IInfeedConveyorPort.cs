namespace ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;

/// <summary>
/// 入口输送线端口接口
/// 对应物理连接：入口段输送线驱动器（控制包裹进入速度）
/// </summary>
public interface IInfeedConveyorPort
{
    /// <summary>
    /// 获取当前输送线速度
    /// </summary>
    /// <returns>当前速度（mm/s）</returns>
    double GetCurrentSpeed();

    /// <summary>
    /// 设置输送线速度
    /// </summary>
    /// <param name="speedMmPerSec">目标速度（mm/s）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否设置成功</returns>
    Task<bool> SetSpeedAsync(double speedMmPerSec, CancellationToken cancellationToken = default);

    /// <summary>
    /// 启动输送线
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否启动成功</returns>
    Task<bool> StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 停止输送线
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否停止成功</returns>
    Task<bool> StopAsync(CancellationToken cancellationToken = default);
}

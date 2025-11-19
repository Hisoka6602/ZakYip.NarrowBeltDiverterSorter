namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.MainLine;

/// <summary>
/// 主线控制服务接口
/// 负责管理主线速度控制和启停
/// </summary>
public interface IMainLineControlService
{
    /// <summary>
    /// 设置目标速度
    /// </summary>
    /// <param name="targetSpeedMmps">目标速度（mm/s）</param>
    void SetTargetSpeed(decimal targetSpeedMmps);

    /// <summary>
    /// 获取当前目标速度
    /// </summary>
    /// <returns>目标速度（mm/s）</returns>
    decimal GetTargetSpeed();

    /// <summary>
    /// 启动控制
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否启动成功</returns>
    Task<bool> StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 停止控制
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否停止成功</returns>
    Task<bool> StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 执行一次控制循环
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>控制是否正常执行</returns>
    Task<bool> ExecuteControlLoopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 控制是否正在运行
    /// </summary>
    bool IsRunning { get; }
}

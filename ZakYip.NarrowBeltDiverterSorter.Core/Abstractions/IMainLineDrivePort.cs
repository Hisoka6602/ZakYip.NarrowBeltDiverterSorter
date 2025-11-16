using ZakYip.NarrowBeltDiverterSorter.Core.Domain;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;

/// <summary>
/// 主驱动线驱动端口接口
/// 对应物理连接：主线驱动器（通常是变频器或伺服驱动器）
/// </summary>
public interface IMainLineDrivePort
{
    /// <summary>
    /// 设置目标线速
    /// </summary>
    /// <param name="speedMmPerSec">目标线速（mm/s）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否设置成功</returns>
    Task<bool> SetTargetSpeedAsync(double speedMmPerSec, CancellationToken cancellationToken = default);

    /// <summary>
    /// 启动主线
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否启动成功</returns>
    Task<bool> StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 停止主线
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否停止成功</returns>
    Task<bool> StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 急停主线
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否急停成功</returns>
    Task<bool> EmergencyStopAsync(CancellationToken cancellationToken = default);
}

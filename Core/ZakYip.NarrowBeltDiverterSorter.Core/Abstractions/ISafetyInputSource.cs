namespace ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;

/// <summary>
/// 安全输入源接口
/// 专门用于安全编排的输入信号（急停、安全门、联锁等）
/// </summary>
public interface ISafetyInputSource
{
    /// <summary>
    /// 读取急停状态
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>true表示急停被触发，false表示正常</returns>
    Task<bool> ReadEmergencyStopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 读取安全门状态
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>true表示安全门关闭（正常），false表示安全门打开（异常）</returns>
    Task<bool> ReadSafetyDoorAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 读取联锁状态
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>true表示联锁正常，false表示联锁异常</returns>
    Task<bool> ReadInterlockAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查所有安全输入是否正常
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>true表示所有安全输入正常，false表示存在安全异常</returns>
    Task<bool> IsAllSafeAsync(CancellationToken cancellationToken = default);
}

namespace ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;

/// <summary>
/// 小车参数端口接口
/// 对应物理连接：小车参数配置通道（所有小车共用参数，不支持单个小车控制）
/// 注：所有小车共用同一套参数配置，不支持针对单个小车的独立控制
/// </summary>
public interface ICartParameterPort
{
    /// <summary>
    /// 设置吐件距离（所有小车共用）
    /// </summary>
    /// <param name="distanceMm">吐件距离（mm）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否设置成功</returns>
    Task<bool> SetEjectionDistanceAsync(double distanceMm, CancellationToken cancellationToken = default);

    /// <summary>
    /// 设置吐件延迟（所有小车共用）
    /// </summary>
    /// <param name="delayMs">吐件延迟（毫秒）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否设置成功</returns>
    Task<bool> SetEjectionDelayAsync(int delayMs, CancellationToken cancellationToken = default);

    /// <summary>
    /// 设置最大连续动作小车数（所有小车共用）
    /// </summary>
    /// <param name="maxCount">最大连续动作小车数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否设置成功</returns>
    Task<bool> SetMaxConsecutiveActionCartsAsync(int maxCount, CancellationToken cancellationToken = default);
}

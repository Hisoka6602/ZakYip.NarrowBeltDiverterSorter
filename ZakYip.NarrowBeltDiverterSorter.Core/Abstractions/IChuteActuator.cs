namespace ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;

/// <summary>
/// 格口执行器抽象接口
/// 用于触发格口的开闭动作，支持不同类型的 IO 策略（脉冲型或保持型）
/// </summary>
public interface IChuteActuator
{
    /// <summary>
    /// 触发指定格口的吐件动作
    /// 内部将根据策略决定是"窗口内保持"还是"脉冲"
    /// </summary>
    /// <param name="chuteId">格口ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    ValueTask TriggerAsync(long chuteId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 关闭指定格口
    /// 对"保持型 IO"有效；对"脉冲型 IO"可以为空实现
    /// </summary>
    /// <param name="chuteId">格口ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    ValueTask CloseAsync(long chuteId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 关闭所有格口
    /// 用于启动/停机/异常时的安全互锁
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    ValueTask CloseAllAsync(CancellationToken cancellationToken = default);
}

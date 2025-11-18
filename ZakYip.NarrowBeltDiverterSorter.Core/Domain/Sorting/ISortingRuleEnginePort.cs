namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.Sorting;

/// <summary>
/// 分拣规则引擎端口接口
/// 定义与规则引擎（RuleEngine）通讯的统一端口，不暴露具体协议（MQTT/TCP/HTTP）
/// 该端口位于 Core 层，确保业务逻辑不依赖具体的通讯实现
/// </summary>
public interface ISortingRuleEnginePort
{
    /// <summary>
    /// 请求分拣规则
    /// 向规则引擎请求为指定包裹分配格口
    /// </summary>
    /// <param name="eventArgs">分拣请求事件参数</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>分配的格口编号，如果失败则返回默认格口</returns>
    ValueTask<int> RequestSortingAsync(SortingRequestEventArgs eventArgs, CancellationToken ct = default);

    /// <summary>
    /// 通知分拣结果确认
    /// 向规则引擎报告包裹的分拣结果（成功或失败）
    /// </summary>
    /// <param name="eventArgs">分拣结果确认事件参数</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>异步任务</returns>
    ValueTask NotifySortingResultAckAsync(SortingResultAckEventArgs eventArgs, CancellationToken ct = default);
}

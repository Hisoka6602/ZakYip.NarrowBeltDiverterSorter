namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.Sorting;

/// <summary>
/// 规则引擎端口接口
/// 定义与上游规则引擎通信的统一端口，不暴露具体协议（MQTT/TCP/HTTP）
/// </summary>
/// <remarks>
/// 此端口遵循六边形架构的端口-适配器模式，由 Core 层定义接口，
/// 由 Communication 层提供具体实现（适配器）
/// </remarks>
public interface ISortingRuleEnginePort
{
    /// <summary>
    /// 请求分拣（向规则引擎请求包裹的格口分配）
    /// </summary>
    /// <param name="eventArgs">分拣请求事件参数</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>异步任务</returns>
    ValueTask RequestSortingAsync(SortingRequestEventArgs eventArgs, CancellationToken ct = default);

    /// <summary>
    /// 通知分拣结果确认（向规则引擎报告分拣结果）
    /// </summary>
    /// <param name="eventArgs">分拣结果确认事件参数</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>异步任务</returns>
    ValueTask NotifySortingResultAckAsync(SortingResultAckEventArgs eventArgs, CancellationToken ct = default);
}

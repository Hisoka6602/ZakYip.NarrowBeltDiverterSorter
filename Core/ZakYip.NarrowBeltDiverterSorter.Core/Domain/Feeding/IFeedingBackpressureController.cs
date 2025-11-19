namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.Feeding;

/// <summary>
/// 供包背压控制器接口
/// 负责根据当前负载情况决策是否允许创建新包裹
/// </summary>
public interface IFeedingBackpressureController
{
    /// <summary>
    /// 检查是否允许创建新包裹
    /// </summary>
    /// <returns>供包决策结果</returns>
    FeedingDecisionResult CheckFeedingAllowed();

    /// <summary>
    /// 记录一次节流事件
    /// </summary>
    void RecordThrottleEvent();

    /// <summary>
    /// 记录一次暂停事件
    /// </summary>
    void RecordPauseEvent();

    /// <summary>
    /// 获取节流次数统计
    /// </summary>
    long GetThrottleCount();

    /// <summary>
    /// 获取暂停次数统计
    /// </summary>
    long GetPauseCount();

    /// <summary>
    /// 重置统计计数器
    /// </summary>
    void ResetCounters();
}

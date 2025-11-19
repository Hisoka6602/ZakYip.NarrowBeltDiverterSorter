namespace ZakYip.NarrowBeltDiverterSorter.Observability.Logging;

/// <summary>
/// 日志节流器接口，用于限制高频日志输出
/// </summary>
public interface ILogThrottler
{
    /// <summary>
    /// 判断是否应该输出日志
    /// </summary>
    /// <param name="messageTemplate">日志消息模板</param>
    /// <param name="keyFields">关键字段组合，用于区分不同的日志实例</param>
    /// <returns>如果应该输出日志返回 true，否则返回 false</returns>
    bool ShouldLog(string messageTemplate, params object[] keyFields);

    /// <summary>
    /// 清空所有节流状态
    /// </summary>
    void Clear();
}

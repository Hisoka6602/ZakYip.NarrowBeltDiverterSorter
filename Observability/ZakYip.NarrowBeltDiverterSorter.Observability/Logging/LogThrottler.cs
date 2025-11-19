using System.Collections.Concurrent;

namespace ZakYip.NarrowBeltDiverterSorter.Observability.Logging;

/// <summary>
/// 日志节流器实现，基于消息模板和关键字段组合进行去重，默认最小间隔 1 秒
/// </summary>
public sealed class LogThrottler : ILogThrottler
{
    private readonly ConcurrentDictionary<string, DateTime> _lastLogTimes = new();
    private readonly TimeSpan _minimumInterval;
    private readonly Func<DateTime> _timeProvider;

    /// <summary>
    /// 创建日志节流器实例
    /// </summary>
    /// <param name="minimumInterval">最小日志间隔，默认 1 秒</param>
    /// <param name="timeProvider">时间提供器，用于测试注入</param>
    public LogThrottler(TimeSpan? minimumInterval = null, Func<DateTime>? timeProvider = null)
    {
        _minimumInterval = minimumInterval ?? TimeSpan.FromSeconds(1);
        _timeProvider = timeProvider ?? (() => DateTime.Now);
    }

    /// <summary>
    /// 判断是否应该输出日志
    /// </summary>
    /// <param name="messageTemplate">日志消息模板</param>
    /// <param name="keyFields">关键字段组合，用于区分不同的日志实例</param>
    /// <returns>如果应该输出日志返回 true，否则返回 false</returns>
    public bool ShouldLog(string messageTemplate, params object[] keyFields)
    {
        if (string.IsNullOrEmpty(messageTemplate))
        {
            return true;
        }

        // 生成唯一键：消息模板 + 关键字段
        var key = GenerateKey(messageTemplate, keyFields);
        var now = _timeProvider();

        // 使用 AddOrUpdate 保证线程安全的原子操作
        var shouldLog = false;
        _lastLogTimes.AddOrUpdate(
            key,
            _ =>
            {
                shouldLog = true;
                return now;
            },
            (_, lastTime) =>
            {
                if (now - lastTime >= _minimumInterval)
                {
                    shouldLog = true;
                    return now;
                }
                return lastTime;
            });

        return shouldLog;
    }

    /// <summary>
    /// 清空所有节流状态
    /// </summary>
    public void Clear()
    {
        _lastLogTimes.Clear();
    }

    /// <summary>
    /// 生成唯一键
    /// </summary>
    private static string GenerateKey(string messageTemplate, object[] keyFields)
    {
        if (keyFields.Length == 0)
        {
            return messageTemplate;
        }

        var fieldsKey = string.Join("|", keyFields.Select(f => f?.ToString() ?? "null"));
        return $"{messageTemplate}::{fieldsKey}";
    }
}

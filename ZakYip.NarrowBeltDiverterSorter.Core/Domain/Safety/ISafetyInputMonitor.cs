using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Safety;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.Safety;

/// <summary>
/// 安全输入监控器接口
/// 负责监控并上报各种安全输入信号的变化
/// </summary>
public interface ISafetyInputMonitor
{
    /// <summary>
    /// 安全输入变化事件（已废弃，请订阅 IEventBus）
    /// </summary>
    [Obsolete("请使用 IEventBus 订阅 Observability.Events.SafetyInputChangedEventArgs，此事件将在未来版本中移除")]
    event EventHandler<SafetyInputChangedEventArgs>? SafetyInputChanged;

    /// <summary>
    /// 启动监控
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    Task StartMonitoringAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 停止监控
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    Task StopMonitoringAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取当前所有安全输入的状态
    /// </summary>
    /// <returns>安全输入源到状态的映射</returns>
    IDictionary<string, bool> GetCurrentSafetyInputStates();
}

namespace ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;

/// <summary>
/// 事件总线接口
/// 提供泛型事件的发布和订阅功能
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// 订阅事件
    /// </summary>
    /// <typeparam name="TEventArgs">事件参数类型</typeparam>
    /// <param name="handler">事件处理器</param>
    void Subscribe<TEventArgs>(Func<TEventArgs, CancellationToken, Task> handler) 
        where TEventArgs : class;

    /// <summary>
    /// 取消订阅事件
    /// </summary>
    /// <typeparam name="TEventArgs">事件参数类型</typeparam>
    /// <param name="handler">事件处理器</param>
    void Unsubscribe<TEventArgs>(Func<TEventArgs, CancellationToken, Task> handler)
        where TEventArgs : class;

    /// <summary>
    /// 发布事件（异步）
    /// </summary>
    /// <typeparam name="TEventArgs">事件参数类型</typeparam>
    /// <param name="eventArgs">事件参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task PublishAsync<TEventArgs>(TEventArgs eventArgs, CancellationToken cancellationToken = default)
        where TEventArgs : class;

    /// <summary>
    /// 获取事件总线积压量（待处理的事件数量）
    /// </summary>
    int GetBacklogCount();
}

using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;

namespace ZakYip.NarrowBeltDiverterSorter.Observability;

/// <summary>
/// 内存事件总线实现
/// 使用 Channel 实现异步事件处理，确保订阅者异常不影响其他订阅者
/// </summary>
public class InMemoryEventBus : IEventBus, IDisposable
{
    private readonly ILogger<InMemoryEventBus> _logger;
    private readonly Dictionary<Type, List<Delegate>> _subscribers = new();
    private readonly Channel<EventWrapper> _eventChannel;
    private readonly CancellationTokenSource _processingCts = new();
    private readonly Task _processingTask;
    private readonly object _lock = new();
    private int _backlogCount = 0;

    /// <summary>
    /// 事件包装器
    /// </summary>
    private record EventWrapper(Type EventType, object EventArgs, CancellationToken CancellationToken);

    public InMemoryEventBus(ILogger<InMemoryEventBus> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // 创建无界通道用于事件处理
        _eventChannel = Channel.CreateUnbounded<EventWrapper>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        // 启动事件处理任务
        _processingTask = Task.Run(ProcessEventsAsync);
        
        _logger.LogInformation("事件总线已启动");
    }

    /// <inheritdoc/>
    public void Subscribe<TEventArgs>(Func<TEventArgs, CancellationToken, Task> handler) 
        where TEventArgs : class
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        var eventType = typeof(TEventArgs);
        
        lock (_lock)
        {
            if (!_subscribers.ContainsKey(eventType))
            {
                _subscribers[eventType] = new List<Delegate>();
            }
            
            _subscribers[eventType].Add(handler);
        }
        
        _logger.LogDebug("已添加事件订阅: {EventType}", eventType.Name);
    }

    /// <inheritdoc/>
    public void Unsubscribe<TEventArgs>(Func<TEventArgs, CancellationToken, Task> handler)
        where TEventArgs : class
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        var eventType = typeof(TEventArgs);
        
        lock (_lock)
        {
            if (_subscribers.TryGetValue(eventType, out var handlers))
            {
                handlers.Remove(handler);
                
                if (handlers.Count == 0)
                {
                    _subscribers.Remove(eventType);
                }
            }
        }
        
        _logger.LogDebug("已移除事件订阅: {EventType}", eventType.Name);
    }

    /// <inheritdoc/>
    public async Task PublishAsync<TEventArgs>(TEventArgs eventArgs, CancellationToken cancellationToken = default)
        where TEventArgs : class
    {
        if (eventArgs == null)
            throw new ArgumentNullException(nameof(eventArgs));

        var eventType = typeof(TEventArgs);
        var wrapper = new EventWrapper(eventType, eventArgs, cancellationToken);

        Interlocked.Increment(ref _backlogCount);
        await _eventChannel.Writer.WriteAsync(wrapper, cancellationToken);
        
        _logger.LogTrace("事件已发布到队列: {EventType}", eventType.Name);
    }

    /// <inheritdoc/>
    public int GetBacklogCount()
    {
        return _backlogCount;
    }

    /// <summary>
    /// 异步处理事件队列
    /// </summary>
    private async Task ProcessEventsAsync()
    {
        _logger.LogInformation("事件处理循环已启动");

        try
        {
            await foreach (var wrapper in _eventChannel.Reader.ReadAllAsync(_processingCts.Token))
            {
                await ProcessEventAsync(wrapper);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("事件处理循环已停止");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "事件处理循环发生未处理异常");
        }
    }

    /// <summary>
    /// 处理单个事件
    /// </summary>
    private async Task ProcessEventAsync(EventWrapper wrapper)
    {
        // 减少积压计数
        Interlocked.Decrement(ref _backlogCount);
        
        List<Delegate>? handlers;
        
        lock (_lock)
        {
            if (!_subscribers.TryGetValue(wrapper.EventType, out var subscriberList))
            {
                // 没有订阅者
                return;
            }
            
            // 复制订阅者列表，避免在处理期间列表被修改
            handlers = new List<Delegate>(subscriberList);
        }

        if (handlers.Count == 0)
        {
            return;
        }

        _logger.LogTrace("开始处理事件: {EventType}, 订阅者数量: {SubscriberCount}", 
            wrapper.EventType.Name, handlers.Count);

        var successCount = 0;
        var failureCount = 0;

        // 并行调用所有订阅者
        var tasks = handlers.Select(async handler =>
        {
            try
            {
                // 调用处理器
                var task = (Task)handler.DynamicInvoke(wrapper.EventArgs, wrapper.CancellationToken)!;
                await task;
                
                Interlocked.Increment(ref successCount);
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref failureCount);
                
                // 捕获异常，记录日志，不影响其他订阅者
                _logger.LogError(ex, 
                    "事件订阅者处理失败: {EventType}, 处理器类型: {HandlerType}", 
                    wrapper.EventType.Name, 
                    handler.Method.DeclaringType?.Name ?? "未知");
            }
        });

        await Task.WhenAll(tasks);

        if (failureCount > 0)
        {
            _logger.LogWarning(
                "事件处理完成: {EventType}, 成功: {SuccessCount}, 失败: {FailureCount}",
                wrapper.EventType.Name, successCount, failureCount);
        }
        else
        {
            _logger.LogTrace(
                "事件处理完成: {EventType}, 成功: {SuccessCount}",
                wrapper.EventType.Name, successCount);
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        _logger.LogInformation("正在关闭事件总线...");
        
        // 停止接收新事件
        _eventChannel.Writer.Complete();
        
        // 取消处理任务
        _processingCts.Cancel();
        
        try
        {
            // 等待处理任务完成
            _processingTask.Wait(TimeSpan.FromSeconds(5));
        }
        catch (AggregateException)
        {
            // 忽略取消异常
        }
        
        _processingCts.Dispose();
        
        _logger.LogInformation("事件总线已关闭");
    }
}

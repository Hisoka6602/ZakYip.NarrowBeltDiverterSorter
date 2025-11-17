namespace ZakYip.NarrowBeltDiverterSorter.Observability.Recording;

/// <summary>
/// 事件录制器接口
/// 用于向当前录制会话追加事件
/// </summary>
public interface IEventRecorder
{
    /// <summary>
    /// 录制事件
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="eventType">事件类型名称</param>
    /// <param name="payload">事件载荷</param>
    /// <param name="timestamp">事件时间戳</param>
    /// <param name="correlationId">关联ID（可选）</param>
    /// <param name="ct">取消令牌</param>
    ValueTask RecordAsync<TEvent>(
        string eventType, 
        TEvent payload, 
        DateTimeOffset timestamp, 
        string? correlationId = null,
        CancellationToken ct = default) where TEvent : class;
}

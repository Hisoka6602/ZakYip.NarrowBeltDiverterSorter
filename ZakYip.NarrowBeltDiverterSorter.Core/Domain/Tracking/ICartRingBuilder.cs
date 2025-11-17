namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.Tracking;

/// <summary>
/// 小车通过事件参数
/// </summary>
public class CartPassedEventArgs : EventArgs
{
    /// <summary>
    /// 小车ID
    /// </summary>
    public required CartId CartId { get; init; }

    /// <summary>
    /// 通过时间
    /// </summary>
    public required DateTimeOffset PassAt { get; init; }
}

/// <summary>
/// 小车环构建器接口
/// </summary>
public interface ICartRingBuilder
{
    /// <summary>
    /// 当前快照（如果已完成构建）
    /// </summary>
    CartRingSnapshot? CurrentSnapshot { get; }

    /// <summary>
    /// 小车通过事件（在检测到小车通过时触发）
    /// </summary>
    event EventHandler<CartPassedEventArgs>? OnCartPassed;

    /// <summary>
    /// 处理原点传感器触发事件
    /// </summary>
    /// <param name="isFirstSensor">是否为第一个传感器</param>
    /// <param name="isRisingEdge">是否为上升沿</param>
    /// <param name="timestamp">时间戳</param>
    void OnOriginSensorTriggered(bool isFirstSensor, bool isRisingEdge, DateTimeOffset timestamp);
}

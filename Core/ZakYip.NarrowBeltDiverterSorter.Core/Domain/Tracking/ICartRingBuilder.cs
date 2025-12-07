namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.Tracking;

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
    /// 处理原点传感器触发事件
    /// </summary>
    /// <param name="isFirstSensor">是否为第一个传感器</param>
    /// <param name="isRisingEdge">是否为上升沿</param>
    /// <param name="timestamp">时间戳</param>
    void OnOriginSensorTriggered(bool isFirstSensor, bool isRisingEdge, DateTimeOffset timestamp);
}

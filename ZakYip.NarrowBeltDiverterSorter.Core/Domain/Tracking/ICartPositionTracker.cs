namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.Tracking;

/// <summary>
/// 小车位置跟踪器接口
/// 跟踪原点位置当前的小车，用于计算各格口对应的小车
/// </summary>
public interface ICartPositionTracker
{
    /// <summary>
    /// 获取小车跟踪器是否已初始化
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// 获取当前原点位置的小车索引
    /// </summary>
    CartIndex? CurrentOriginCartIndex { get; }

    /// <summary>
    /// 原点传感器触发事件（小车经过原点）
    /// </summary>
    /// <param name="timestamp">时间戳</param>
    void OnCartPassedOrigin(DateTimeOffset timestamp);

    /// <summary>
    /// 根据偏移量计算小车索引
    /// </summary>
    /// <param name="offset">相对原点的偏移量（格口编号）</param>
    /// <param name="ringLength">环长度</param>
    /// <returns>计算出的小车索引</returns>
    CartIndex? CalculateCartIndexAtOffset(int offset, RingLength ringLength);
}

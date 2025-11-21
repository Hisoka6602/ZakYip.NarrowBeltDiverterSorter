namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.Parcels;

/// <summary>
/// 包裹路由完成事件参数
/// 当包裹从上游获得格口分配后触发
/// </summary>
public class ParcelRoutedEventArgs : EventArgs
{
    /// <summary>
    /// 包裹ID
    /// </summary>
    public required ParcelId ParcelId { get; init; }

    /// <summary>
    /// 分配的格口ID（如果成功）
    /// </summary>
    public ChuteId? ChuteId { get; init; }

    /// <summary>
    /// 路由是否成功
    /// </summary>
    public required bool IsSuccess { get; init; }

    /// <summary>
    /// 错误消息（如果失败）
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// 路由时间（本地时间）
    /// </summary>
    public DateTimeOffset RoutedTime { get; init; } = DateTimeOffset.Now;
}

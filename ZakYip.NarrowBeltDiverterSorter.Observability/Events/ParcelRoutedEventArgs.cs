namespace ZakYip.NarrowBeltDiverterSorter.Observability.Events;

/// <summary>
/// 包裹路由完成事件参数（用于事件总线）
/// </summary>
public record class ParcelRoutedEventArgs
{
    /// <summary>
    /// 包裹ID
    /// </summary>
    public required long ParcelId { get; init; }

    /// <summary>
    /// 格口ID
    /// </summary>
    public required long ChuteId { get; init; }

    /// <summary>
    /// 是否成功
    /// </summary>
    public required bool IsSuccess { get; init; }

    /// <summary>
    /// 消息
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// 路由时间
    /// </summary>
    public required DateTimeOffset RoutedAt { get; init; }
}

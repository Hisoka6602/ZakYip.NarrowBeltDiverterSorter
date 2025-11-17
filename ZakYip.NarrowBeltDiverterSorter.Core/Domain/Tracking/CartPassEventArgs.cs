namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.Tracking;

/// <summary>
/// 小车通过事件参数
/// 用于小车自检算法，记录小车通过某个检测点（如原点传感器）的信息
/// </summary>
public readonly record struct CartPassEventArgs
{
    /// <summary>
    /// 小车ID
    /// 来源：从小车环中识别出的小车编号
    /// </summary>
    public required int CartId { get; init; }

    /// <summary>
    /// 通过时间戳
    /// 来源：检测到小车通过的时间点
    /// </summary>
    public required DateTimeOffset PassAt { get; init; }

    /// <summary>
    /// 主线速度（mm/s）
    /// 来源：小车通过时的主线当前速度
    /// </summary>
    public required decimal LineSpeedMmps { get; init; }
}

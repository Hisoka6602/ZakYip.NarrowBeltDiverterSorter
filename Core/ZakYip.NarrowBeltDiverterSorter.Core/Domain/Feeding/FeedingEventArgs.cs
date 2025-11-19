namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.Feeding;

/// <summary>
/// 从入口创建包裹事件参数
/// </summary>
public class ParcelCreatedFromInfeedEventArgs : EventArgs
{
    /// <summary>
    /// 包裹ID
    /// </summary>
    public required ParcelId ParcelId { get; init; }

    /// <summary>
    /// 条码
    /// </summary>
    public required string Barcode { get; init; }

    /// <summary>
    /// 入口触发时间
    /// </summary>
    public required DateTimeOffset InfeedTriggerTime { get; init; }
}

/// <summary>
/// 包裹装载到小车事件参数
/// </summary>
public class ParcelLoadedOnCartEventArgs : EventArgs
{
    /// <summary>
    /// 包裹ID
    /// </summary>
    public required ParcelId ParcelId { get; init; }

    /// <summary>
    /// 小车ID
    /// </summary>
    public required CartId CartId { get; init; }

    /// <summary>
    /// 落车时间
    /// </summary>
    public required DateTimeOffset LoadedTime { get; init; }
}

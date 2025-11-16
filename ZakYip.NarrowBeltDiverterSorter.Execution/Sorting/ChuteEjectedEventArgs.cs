using ZakYip.NarrowBeltDiverterSorter.Core.Domain;

namespace ZakYip.NarrowBeltDiverterSorter.Execution.Sorting;

/// <summary>
/// 格口动作触发事件参数
/// 在仿真模式下，当小车到达格口时间窗口时触发
/// </summary>
public class ChuteEjectedEventArgs : EventArgs
{
    /// <summary>
    /// 格口ID
    /// </summary>
    public required ChuteId ChuteId { get; init; }

    /// <summary>
    /// 小车ID
    /// </summary>
    public required CartId CartId { get; init; }

    /// <summary>
    /// 包裹ID（可选）
    /// </summary>
    public ParcelId? ParcelId { get; init; }

    /// <summary>
    /// 触发时间
    /// </summary>
    public required DateTimeOffset TriggeredAt { get; init; }

    /// <summary>
    /// 是否为强排口
    /// </summary>
    public bool IsForceEject { get; init; }
}

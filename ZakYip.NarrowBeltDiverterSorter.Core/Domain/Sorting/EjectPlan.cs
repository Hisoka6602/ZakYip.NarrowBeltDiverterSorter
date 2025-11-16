namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.Sorting;

/// <summary>
/// 吐件计划
/// 描述在何时打开哪个格口的发信器以及持续时长
/// </summary>
public record class EjectPlan
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
    /// 格口ID
    /// </summary>
    public required ChuteId ChuteId { get; init; }

    /// <summary>
    /// 发信器开启时间
    /// </summary>
    public required DateTimeOffset OpenAt { get; init; }

    /// <summary>
    /// 发信器开启持续时长
    /// </summary>
    public required TimeSpan OpenDuration { get; init; }

    /// <summary>
    /// 是否为强制吐件（强排口）
    /// </summary>
    public bool IsForceEject { get; init; }
}

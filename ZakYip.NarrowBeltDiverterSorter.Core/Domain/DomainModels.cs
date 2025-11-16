namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain;

/// <summary>
/// 包裹快照
/// </summary>
public record class ParcelSnapshot
{
    /// <summary>
    /// 包裹ID
    /// </summary>
    public required ParcelId ParcelId { get; init; }

    /// <summary>
    /// 目标格口ID
    /// </summary>
    public ChuteId? TargetChuteId { get; init; }

    /// <summary>
    /// 绑定的小车ID
    /// </summary>
    public CartId? BoundCartId { get; init; }

    /// <summary>
    /// 路由状态
    /// </summary>
    public ParcelRouteState RouteState { get; init; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// 装载时间
    /// </summary>
    public DateTimeOffset? LoadedAt { get; init; }

    /// <summary>
    /// 分拣完成时间
    /// </summary>
    public DateTimeOffset? SortedAt { get; init; }
}

/// <summary>
/// 小车快照
/// </summary>
public record class CartSnapshot
{
    /// <summary>
    /// 小车ID
    /// </summary>
    public required CartId CartId { get; init; }

    /// <summary>
    /// 小车索引
    /// </summary>
    public required CartIndex CartIndex { get; init; }

    /// <summary>
    /// 是否已装载
    /// </summary>
    public bool IsLoaded { get; init; }

    /// <summary>
    /// 当前包裹ID
    /// </summary>
    public ParcelId? CurrentParcelId { get; init; }

    /// <summary>
    /// 上次复位时间
    /// </summary>
    public DateTimeOffset LastResetAt { get; init; }
}

/// <summary>
/// 格口配置
/// </summary>
public record class ChuteConfig
{
    /// <summary>
    /// 格口ID
    /// </summary>
    public required ChuteId ChuteId { get; init; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; init; }

    /// <summary>
    /// 是否强制弹出
    /// </summary>
    public bool IsForceEject { get; init; }

    /// <summary>
    /// 相对原点小车偏移
    /// </summary>
    public int CartOffsetFromOrigin { get; init; }

    /// <summary>
    /// 最大打开持续时间
    /// </summary>
    public TimeSpan MaxOpenDuration { get; init; }
}

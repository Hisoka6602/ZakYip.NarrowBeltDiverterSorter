namespace ZakYip.NarrowBeltDiverterSorter.Observability.LiveView;

/// <summary>
/// 最后分拣请求快照
/// </summary>
public record class LastSortingRequestSnapshot
{
    /// <summary>
    /// 包裹ID
    /// </summary>
    public long ParcelId { get; init; }

    /// <summary>
    /// 条码
    /// </summary>
    public string? Barcode { get; init; }

    /// <summary>
    /// 购物车编号
    /// </summary>
    public int? CartNumber { get; init; }

    /// <summary>
    /// 请求时间
    /// </summary>
    public DateTimeOffset RequestTime { get; init; }
}

/// <summary>
/// 最后分拣结果快照
/// </summary>
public record class LastSortingResultSnapshot
{
    /// <summary>
    /// 包裹ID
    /// </summary>
    public long ParcelId { get; init; }

    /// <summary>
    /// 格口编号
    /// </summary>
    public int ChuteNumber { get; init; }

    /// <summary>
    /// 购物车计数
    /// </summary>
    public int? CartCount { get; init; }

    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// 失败原因（如果失败）
    /// </summary>
    public string? FailureReason { get; init; }

    /// <summary>
    /// 结果时间
    /// </summary>
    public DateTimeOffset ResultTime { get; init; }
}

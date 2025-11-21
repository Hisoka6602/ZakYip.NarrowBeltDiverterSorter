namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.Sorting;

/// <summary>
/// 分拣请求事件参数
/// 用于向规则引擎请求包裹的格口分配
/// </summary>
/// <remarks>
/// 使用 record class 以获得值类型语义和不可变性
/// 字段类型避免使用 double，优先使用 decimal 以保证精度
/// </remarks>
public record class SortingRequestEventArgs
{
    /// <summary>
    /// 包裹ID（毫秒时间戳）
    /// </summary>
    public required long ParcelId { get; init; }

    /// <summary>
    /// 购物车编号（可选）
    /// </summary>
    public int? CartNumber { get; init; }

    /// <summary>
    /// 条码（可选）
    /// </summary>
    public string? Barcode { get; init; }

    /// <summary>
    /// 重量（kg，可选）
    /// </summary>
    public decimal? Weight { get; init; }

    /// <summary>
    /// 长度（cm，可选）
    /// </summary>
    public decimal? Length { get; init; }

    /// <summary>
    /// 宽度（cm，可选）
    /// </summary>
    public decimal? Width { get; init; }

    /// <summary>
    /// 高度（cm，可选）
    /// </summary>
    public decimal? Height { get; init; }

    /// <summary>
    /// 体积（cm³，可选）
    /// </summary>
    public decimal? Volume { get; init; }

    /// <summary>
    /// 请求时间（本地时间）
    /// </summary>
    public DateTimeOffset RequestTime { get; init; } = DateTimeOffset.Now;
}

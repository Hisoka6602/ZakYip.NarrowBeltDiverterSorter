namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.Sorting;

/// <summary>
/// 分拣请求事件参数
/// 用于向规则引擎请求格口分配时的事件载荷
/// </summary>
public record class SortingRequestEventArgs
{
    /// <summary>
    /// 包裹ID（毫秒时间戳）
    /// </summary>
    public required long ParcelId { get; init; }

    /// <summary>
    /// 小车编号
    /// </summary>
    public required int CartNumber { get; init; }

    /// <summary>
    /// 条码（可选）
    /// </summary>
    public string? Barcode { get; init; }

    /// <summary>
    /// 重量（千克），使用 decimal 避免浮点精度问题
    /// </summary>
    public decimal? WeightKg { get; init; }

    /// <summary>
    /// 长度（厘米）
    /// </summary>
    public decimal? LengthCm { get; init; }

    /// <summary>
    /// 宽度（厘米）
    /// </summary>
    public decimal? WidthCm { get; init; }

    /// <summary>
    /// 高度（厘米）
    /// </summary>
    public decimal? HeightCm { get; init; }

    /// <summary>
    /// 体积（立方厘米）
    /// </summary>
    public decimal? VolumeCm3 { get; init; }

    /// <summary>
    /// 请求时间
    /// </summary>
    public DateTimeOffset RequestTime { get; init; } = DateTimeOffset.UtcNow;
}

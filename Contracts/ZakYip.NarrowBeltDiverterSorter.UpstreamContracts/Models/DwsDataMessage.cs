namespace ZakYip.NarrowBeltDiverterSorter.UpstreamContracts.Models;

/// <summary>
/// DWS（动态称重扫描）数据消息
/// 用于向规则引擎报告包裹的重量、尺寸等信息
/// </summary>
/// <remarks>
/// 此消息不引用任何 NarrowBelt 内部类型，只使用基础类型，
/// 保证可以在 RuleEngine Core 那边重用/共享
/// 字段类型避免使用 double，使用 decimal 以保证精度
/// </remarks>
public record class DwsDataMessage
{
    /// <summary>
    /// 包裹ID（毫秒时间戳）
    /// </summary>
    public required long ParcelId { get; init; }

    /// <summary>
    /// 条码（可选）
    /// </summary>
    public string? Barcode { get; init; }

    /// <summary>
    /// 重量（kg）
    /// </summary>
    public decimal? Weight { get; init; }

    /// <summary>
    /// 长度（cm）
    /// </summary>
    public decimal? Length { get; init; }

    /// <summary>
    /// 宽度（cm）
    /// </summary>
    public decimal? Width { get; init; }

    /// <summary>
    /// 高度（cm）
    /// </summary>
    public decimal? Height { get; init; }

    /// <summary>
    /// 体积（cm³）
    /// </summary>
    public decimal? Volume { get; init; }

    /// <summary>
    /// 测量时间
    /// </summary>
    public DateTimeOffset MeasuredTime { get; init; } = DateTimeOffset.Now;
}

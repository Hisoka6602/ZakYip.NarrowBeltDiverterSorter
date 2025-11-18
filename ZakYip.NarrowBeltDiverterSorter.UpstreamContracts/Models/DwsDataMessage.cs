namespace ZakYip.NarrowBeltDiverterSorter.UpstreamContracts.Models;

/// <summary>
/// DWS（Dimension Weight Scan）数据消息
/// 用于向规则引擎上报包裹的尺寸、重量等物理信息（与 RuleEngine 文档对齐）
/// </summary>
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
    /// 扫描时间
    /// </summary>
    public DateTimeOffset ScanTime { get; init; } = DateTimeOffset.UtcNow;
}

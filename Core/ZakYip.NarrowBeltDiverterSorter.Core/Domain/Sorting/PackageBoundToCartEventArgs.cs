namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.Sorting;

/// <summary>
/// 包裹绑定到小车事件参数
/// 当包裹创建时绑定到特定小车号时触发
/// </summary>
public record class PackageBoundToCartEventArgs
{
    /// <summary>
    /// 包裹ID
    /// </summary>
    public required long PackageId { get; init; }

    /// <summary>
    /// 小车号（1 基索引）
    /// </summary>
    public required int CartNumber { get; init; }

    /// <summary>
    /// 格口代码
    /// </summary>
    public required string ChuteCode { get; init; }

    /// <summary>
    /// 格口ID
    /// </summary>
    public required long ChuteId { get; init; }

    /// <summary>
    /// 绑定时间
    /// </summary>
    public required DateTimeOffset BoundAt { get; init; }
}

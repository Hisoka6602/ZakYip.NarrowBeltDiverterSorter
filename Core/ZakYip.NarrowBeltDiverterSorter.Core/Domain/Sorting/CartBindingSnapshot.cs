namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.Sorting;

/// <summary>
/// 小车绑定快照
/// 代表某一时刻的小车配置和首车状态的一致性视图
/// </summary>
public sealed record class CartBindingSnapshot
{
    /// <summary>
    /// 小车环上的总小车数量
    /// </summary>
    public required int TotalCartCount { get; init; }

    /// <summary>
    /// 当前原点处的小车编号（1 基索引）
    /// </summary>
    public required int HeadCartNumber { get; init; }

    /// <summary>
    /// 格口ID
    /// </summary>
    public required long ChuteId { get; init; }

    /// <summary>
    /// 当首车在原点时，该格口窗口的小车编号（1 基索引）
    /// </summary>
    public required int CartNumberWhenHeadAtOrigin { get; init; }

    /// <summary>
    /// 快照捕获时间
    /// </summary>
    public required DateTimeOffset CapturedAt { get; init; }
}

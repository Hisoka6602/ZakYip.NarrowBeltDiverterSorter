namespace ZakYip.NarrowBeltDiverterSorter.UpstreamContracts.Models;

/// <summary>
/// EMC锁事件参数
/// 用于传递EMC锁相关事件的数据
/// </summary>
public record class EmcLockEventArgs
{
    /// <summary>
    /// EMC锁事件
    /// </summary>
    public required EmcLockEvent LockEvent { get; init; }
}

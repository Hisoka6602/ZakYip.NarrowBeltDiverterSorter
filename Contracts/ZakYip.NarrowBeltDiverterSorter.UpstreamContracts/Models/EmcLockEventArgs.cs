namespace ZakYip.NarrowBeltDiverterSorter.UpstreamContracts.Models;

/// <summary>
/// EMC锁事件参数
/// 用于传递EMC锁相关事件的数据
/// </summary>
public class EmcLockEventArgs : EventArgs
{
    /// <summary>
    /// EMC锁事件
    /// </summary>
    public EmcLockEvent LockEvent { get; }
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="lockEvent">锁事件</param>
    public EmcLockEventArgs(EmcLockEvent lockEvent)
    {
        LockEvent = lockEvent;
    }
}

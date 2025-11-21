namespace ZakYip.NarrowBeltDiverterSorter.UpstreamContracts.Models;

/// <summary>
/// EMC锁事件
/// 用于在多实例之间协调对EMC硬件资源的访问
/// </summary>
public record class EmcLockEvent
{
    /// <summary>
    /// 事件ID（唯一标识）
    /// </summary>
    public string EventId { get; init; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// 发送者实例ID
    /// </summary>
    public required string InstanceId { get; init; }
    
    /// <summary>
    /// 通知类型
    /// </summary>
    public required EmcLockNotificationType NotificationType { get; init; }
    
    /// <summary>
    /// EMC卡号
    /// </summary>
    public required ushort CardNo { get; init; }
    
    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// 额外消息
    /// </summary>
    public string? Message { get; init; }
    
    /// <summary>
    /// 超时时间（毫秒）- 其他实例需要在此时间内响应
    /// </summary>
    public int TimeoutMs { get; init; } = 5000;
}

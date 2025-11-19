namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.SystemState;

/// <summary>
/// 系统故障记录
/// 表示一个当前活动的系统故障
/// </summary>
public class SystemFault
{
    /// <summary>
    /// 故障代码
    /// </summary>
    public SystemFaultCode FaultCode { get; init; }

    /// <summary>
    /// 故障发生时间
    /// </summary>
    public DateTimeOffset OccurredAt { get; init; }

    /// <summary>
    /// 故障消息
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// 故障是否会阻断系统运行
    /// </summary>
    public bool IsBlocking { get; init; }
}

using ZakYip.NarrowBeltDiverterSorter.Core.Enums.Domain;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.SystemState;

/// <summary>
/// 系统故障记录
/// 表示一个当前活动的系统故障
/// </summary>
public record class SystemFault
{
    /// <summary>
    /// 故障代码
    /// </summary>
    public required SystemFaultCode FaultCode { get; init; }

    /// <summary>
    /// 故障发生时间
    /// </summary>
    public required DateTimeOffset OccurredAt { get; init; }

    /// <summary>
    /// 故障消息
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// 故障是否会阻断系统运行
    /// </summary>
    public required bool IsBlocking { get; init; }
}

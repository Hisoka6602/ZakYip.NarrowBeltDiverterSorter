namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.SystemState;

/// <summary>
/// 系统故障事件参数
/// 描述故障的详细信息
/// </summary>
public record class SystemFaultEventArgs
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
    /// 关联的异常（可选）
    /// </summary>
    public Exception? Exception { get; init; }
}

namespace ZakYip.NarrowBeltDiverterSorter.Host.Contracts;

/// <summary>
/// 系统故障数据传输对象
/// </summary>
public record class SystemFaultDto
{
    /// <summary>
    /// 故障代码
    /// </summary>
    public required string FaultCode { get; init; }

    /// <summary>
    /// 故障消息
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// 故障发生时间
    /// </summary>
    public required DateTimeOffset OccurredAt { get; init; }

    /// <summary>
    /// 是否阻断系统运行
    /// </summary>
    public required bool IsBlocking { get; init; }
}

/// <summary>
/// 获取系统故障响应
/// </summary>
public record class GetSystemFaultsResponse
{
    /// <summary>
    /// 当前活动故障列表
    /// </summary>
    public required List<SystemFaultDto> Faults { get; init; }

    /// <summary>
    /// 是否存在阻断运行的故障
    /// </summary>
    public required bool HasBlockingFault { get; init; }

    /// <summary>
    /// 当前系统状态
    /// </summary>
    public required string CurrentSystemState { get; init; }
}

/// <summary>
/// 复位故障响应
/// </summary>
public record class ResetFaultsResponse
{
    /// <summary>
    /// 操作是否成功
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// 操作消息
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// 清除的故障数量
    /// </summary>
    public required int ClearedFaultCount { get; init; }

    /// <summary>
    /// 新的系统状态
    /// </summary>
    public required string NewSystemState { get; init; }
}

/// <summary>
/// 错误响应（系统故障相关）
/// </summary>
public record class FaultErrorResponse
{
    /// <summary>
    /// 错误消息
    /// </summary>
    public required string Error { get; init; }

    /// <summary>
    /// 当前系统状态
    /// </summary>
    public required string CurrentState { get; init; }
}

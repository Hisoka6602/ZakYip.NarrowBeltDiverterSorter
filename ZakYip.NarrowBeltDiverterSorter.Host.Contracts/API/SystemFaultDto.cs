namespace ZakYip.NarrowBeltDiverterSorter.Host.Contracts;

/// <summary>
/// 系统故障数据传输对象
/// </summary>
public class SystemFaultDto
{
    /// <summary>
    /// 故障代码
    /// </summary>
    public string FaultCode { get; set; } = string.Empty;

    /// <summary>
    /// 故障消息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 故障发生时间
    /// </summary>
    public DateTimeOffset OccurredAt { get; set; }

    /// <summary>
    /// 是否阻断系统运行
    /// </summary>
    public bool IsBlocking { get; set; }
}

/// <summary>
/// 获取系统故障响应
/// </summary>
public class GetSystemFaultsResponse
{
    /// <summary>
    /// 当前活动故障列表
    /// </summary>
    public List<SystemFaultDto> Faults { get; set; } = new();

    /// <summary>
    /// 是否存在阻断运行的故障
    /// </summary>
    public bool HasBlockingFault { get; set; }

    /// <summary>
    /// 当前系统状态
    /// </summary>
    public string CurrentSystemState { get; set; } = string.Empty;
}

/// <summary>
/// 复位故障响应
/// </summary>
public class ResetFaultsResponse
{
    /// <summary>
    /// 操作是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 操作消息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 清除的故障数量
    /// </summary>
    public int ClearedFaultCount { get; set; }

    /// <summary>
    /// 新的系统状态
    /// </summary>
    public string NewSystemState { get; set; } = string.Empty;
}

/// <summary>
/// 错误响应（系统故障相关）
/// </summary>
public class FaultErrorResponse
{
    /// <summary>
    /// 错误消息
    /// </summary>
    public string Error { get; set; } = string.Empty;

    /// <summary>
    /// 当前系统状态
    /// </summary>
    public string CurrentState { get; set; } = string.Empty;
}

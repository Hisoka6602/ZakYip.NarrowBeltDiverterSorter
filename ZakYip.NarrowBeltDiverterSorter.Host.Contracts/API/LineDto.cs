namespace ZakYip.NarrowBeltDiverterSorter.Host.Contracts.API;

/// <summary>
/// 线体状态响应
/// </summary>
public record class LineStateResponse
{
    /// <summary>
    /// 线体运行状态
    /// </summary>
    public required string LineRunState { get; init; }

    /// <summary>
    /// 安全状态
    /// </summary>
    public required string SafetyState { get; init; }

    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTimeOffset Timestamp { get; init; }
}

/// <summary>
/// 线体操作响应
/// </summary>
public record class LineOperationResponse
{
    /// <summary>
    /// 操作是否成功
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// 响应消息
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// 当前线体运行状态
    /// </summary>
    public required string CurrentLineRunState { get; init; }

    /// <summary>
    /// 当前安全状态
    /// </summary>
    public required string CurrentSafetyState { get; init; }

    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTimeOffset Timestamp { get; init; }
}

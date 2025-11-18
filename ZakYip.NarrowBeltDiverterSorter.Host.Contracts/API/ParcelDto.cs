using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ZakYip.NarrowBeltDiverterSorter.Host.Contracts.API;

/// <summary>
/// 包裹生命周期 DTO
/// </summary>
/// <remarks>
/// 描述包裹从创建到完成的完整生命周期信息，包括状态转换、时间戳和路由信息
/// </remarks>
/// <example>
/// {
///   "parcelId": 12345,
///   "status": "Sorted",
///   "failureReason": "None",
///   "routeState": "Executed",
///   "targetChuteId": 10,
///   "actualChuteId": 10,
///   "createdAt": "2024-01-15T10:30:00Z",
///   "sortedAt": "2024-01-15T10:30:05Z"
/// }
/// </example>
public record class ParcelLifecycleDto
{
    /// <summary>
    /// 包裹唯一标识符
    /// </summary>
    /// <example>12345</example>
    [Required]
    public required long ParcelId { get; init; }
    
    /// <summary>
    /// 包裹状态
    /// </summary>
    /// <remarks>
    /// 可能的值: Created (已创建), Loaded (已装载), Sorted (已分拣), Completed (已完成), Discarded (已丢弃)
    /// </remarks>
    /// <example>Sorted</example>
    [Required]
    [DefaultValue("Created")]
    public required string Status { get; init; }
    
    /// <summary>
    /// 失败原因
    /// </summary>
    /// <remarks>
    /// 可能的值: None (无), NoRoute (无路由), CartConflict (小车冲突), Timeout (超时), SystemError (系统错误)
    /// </remarks>
    /// <example>None</example>
    [Required]
    [DefaultValue("None")]
    public required string FailureReason { get; init; }
    
    /// <summary>
    /// 路由状态
    /// </summary>
    /// <remarks>
    /// 可能的值: Pending (待处理), Planned (已计划), Executed (已执行)
    /// </remarks>
    /// <example>Executed</example>
    [Required]
    [DefaultValue("Pending")]
    public required string RouteState { get; init; }
    
    /// <summary>
    /// 目标格口ID
    /// </summary>
    /// <example>10</example>
    public long? TargetChuteId { get; init; }
    
    /// <summary>
    /// 实际落格的格口ID
    /// </summary>
    /// <example>10</example>
    public long? ActualChuteId { get; init; }
    
    /// <summary>
    /// 绑定的小车ID
    /// </summary>
    /// <example>5</example>
    public long? BoundCartId { get; init; }
    
    /// <summary>
    /// 预测的小车ID
    /// </summary>
    /// <example>5</example>
    public long? PredictedCartId { get; init; }
    
    /// <summary>
    /// 包裹创建时间 (UTC)
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    [Required]
    public DateTimeOffset CreatedAt { get; init; }
    
    /// <summary>
    /// 包裹装载时间 (UTC)
    /// </summary>
    /// <example>2024-01-15T10:30:01Z</example>
    public DateTimeOffset? LoadedAt { get; init; }
    
    /// <summary>
    /// 分拣计划时间 (UTC)
    /// </summary>
    /// <example>2024-01-15T10:30:02Z</example>
    public DateTimeOffset? DivertPlannedAt { get; init; }
    
    /// <summary>
    /// 实际落格时间 (UTC)
    /// </summary>
    /// <example>2024-01-15T10:30:05Z</example>
    public DateTimeOffset? DivertedAt { get; init; }
    
    /// <summary>
    /// 分拣完成时间 (UTC)
    /// </summary>
    /// <example>2024-01-15T10:30:05Z</example>
    public DateTimeOffset? SortedAt { get; init; }
    
    /// <summary>
    /// 生命周期完成时间 (UTC)
    /// </summary>
    /// <example>2024-01-15T10:30:06Z</example>
    public DateTimeOffset? CompletedAt { get; init; }
    
    /// <summary>
    /// 分拣结果
    /// </summary>
    /// <remarks>
    /// 可能的值: Success (成功), Failed (失败), Redirected (重定向到异常口)
    /// </remarks>
    /// <example>Success</example>
    [DefaultValue("Success")]
    public string? SortingOutcome { get; init; }
    
    /// <summary>
    /// 丢弃原因（如果被丢弃）
    /// </summary>
    /// <example>Timeout</example>
    public string? DiscardReason { get; init; }
}

/// <summary>
/// 包裹生命周期统计 DTO
/// </summary>
/// <remarks>
/// 提供包裹状态分布和失败原因的统计信息
/// </remarks>
/// <example>
/// {
///   "statusDistribution": {
///     "Sorted": 150,
///     "Completed": 100,
///     "Created": 5
///   },
///   "failureReasonDistribution": {
///     "None": 250,
///     "Timeout": 5
///   },
///   "onlineCount": 5,
///   "totalTracked": 255
/// }
/// </example>
public record class ParcelLifecycleStatsDto
{
    /// <summary>
    /// 各状态的包裹数量分布
    /// </summary>
    /// <remarks>
    /// 键为状态名称，值为该状态的包裹数量
    /// </remarks>
    /// <example>{"Sorted": 150, "Completed": 100, "Created": 5}</example>
    [Required]
    public required Dictionary<string, int> StatusDistribution { get; init; }
    
    /// <summary>
    /// 各失败原因的包裹数量分布
    /// </summary>
    /// <remarks>
    /// 键为失败原因，值为该原因导致失败的包裹数量
    /// </remarks>
    /// <example>{"None": 250, "Timeout": 5}</example>
    [Required]
    public required Dictionary<string, int> FailureReasonDistribution { get; init; }
    
    /// <summary>
    /// 当前在线（未完成）的包裹数量
    /// </summary>
    /// <example>5</example>
    [Required]
    public int OnlineCount { get; init; }
    
    /// <summary>
    /// 总追踪的包裹数量
    /// </summary>
    /// <example>255</example>
    [Required]
    public int TotalTracked { get; init; }
}

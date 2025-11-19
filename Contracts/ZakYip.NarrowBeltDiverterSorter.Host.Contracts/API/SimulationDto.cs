using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ZakYip.NarrowBeltDiverterSorter.Host.Contracts.API;

/// <summary>
/// 长跑仿真启动响应
/// </summary>
/// <remarks>
/// 返回长跑仿真任务的启动结果和配置信息
/// </remarks>
/// <example>
/// {
///   "runId": "lr-20240115-103000-abc123",
///   "status": "triggered",
///   "message": "长跑仿真已通过面板启动按钮触发",
///   "configuration": {
///     "targetParcelCount": 10000,
///     "parcelCreationIntervalMs": 300,
///     "chuteCount": 100,
///     "mainLineSpeedMmps": 2000
///   }
/// }
/// </example>
public class LongRunSimulationStartResponse
{
    /// <summary>
    /// 仿真运行唯一标识符
    /// </summary>
    /// <remarks>
    /// 格式: lr-{yyyyMMdd-HHmmss}-{guid}
    /// </remarks>
    /// <example>lr-20240115-103000-abc123</example>
    [Required]
    [DefaultValue("lr-20240115-103000-abc123")]
    public required string RunId { get; init; }

    /// <summary>
    /// 仿真状态
    /// </summary>
    /// <remarks>
    /// 可能的值: triggered (已触发), running (运行中), completed (已完成), failed (失败)
    /// </remarks>
    /// <example>triggered</example>
    [Required]
    [DefaultValue("triggered")]
    public required string Status { get; init; }

    /// <summary>
    /// 响应消息
    /// </summary>
    /// <remarks>
    /// 提供仿真启动状态的详细说明
    /// </remarks>
    /// <example>长跑仿真已通过面板启动按钮触发</example>
    [Required]
    [DefaultValue("仿真已启动")]
    public required string Message { get; init; }

    /// <summary>
    /// 仿真配置摘要
    /// </summary>
    /// <remarks>
    /// 包含目标包裹数、创建间隔、格口数量、主线速度等关键配置参数
    /// </remarks>
    /// <example>
    /// {
    ///   "targetParcelCount": 10000,
    ///   "parcelCreationIntervalMs": 300,
    ///   "chuteCount": 100,
    ///   "mainLineSpeedMmps": 2000
    /// }
    /// </example>
    [Required]
    public required object Configuration { get; init; }
}

/// <summary>
/// 仿真结果统计 DTO
/// </summary>
/// <remarks>
/// 返回仿真过程中的统计数据，包括总包裹数、分拣成功数、错误数等
/// </remarks>
/// <example>
/// {
///   "runId": "lr-20240115-103000-abc123",
///   "totalParcels": 1000,
///   "sortedToTargetChutes": 950,
///   "sortedToErrorChute": 50,
///   "timedOutCount": 40,
///   "misSortedCount": 0,
///   "isCompleted": true,
///   "startTime": "2024-01-15T10:30:00Z",
///   "endTime": "2024-01-15T10:35:00Z"
/// }
/// </example>
public class SimulationResultDto
{
    /// <summary>
    /// 仿真运行唯一标识符
    /// </summary>
    /// <remarks>
    /// 格式: lr-{yyyyMMdd-HHmmss}-{guid}
    /// </remarks>
    /// <example>lr-20240115-103000-abc123</example>
    [Required]
    [DefaultValue("lr-20240115-103000-abc123")]
    public required string RunId { get; init; }

    /// <summary>
    /// 总包裹数
    /// </summary>
    /// <remarks>
    /// 仿真期间创建的包裹总数
    /// </remarks>
    /// <example>1000</example>
    [Required]
    [DefaultValue(0)]
    public required int TotalParcels { get; init; }

    /// <summary>
    /// 分拣到目标格口的包裹数
    /// </summary>
    /// <remarks>
    /// 成功分拣到预期格口的包裹数量
    /// </remarks>
    /// <example>950</example>
    [Required]
    [DefaultValue(0)]
    public required int SortedToTargetChutes { get; init; }

    /// <summary>
    /// 分拣到异常口的包裹数
    /// </summary>
    /// <remarks>
    /// 由于超时或其他原因被分拣到异常口（强排口）的包裹数量
    /// </remarks>
    /// <example>50</example>
    [Required]
    [DefaultValue(0)]
    public required int SortedToErrorChute { get; init; }

    /// <summary>
    /// 超时的包裹数
    /// </summary>
    /// <remarks>
    /// 在规定时间内未收到上游路由响应的包裹数量
    /// </remarks>
    /// <example>40</example>
    [Required]
    [DefaultValue(0)]
    public required int TimedOutCount { get; init; }

    /// <summary>
    /// 错分的包裹数
    /// </summary>
    /// <remarks>
    /// 分拣到错误格口的包裹数量（实际格口 != 目标格口）
    /// </remarks>
    /// <example>0</example>
    [Required]
    [DefaultValue(0)]
    public required int MisSortedCount { get; init; }

    /// <summary>
    /// 仿真是否已完成
    /// </summary>
    /// <remarks>
    /// true 表示所有包裹已处理完成，false 表示仍在运行中
    /// </remarks>
    /// <example>true</example>
    [Required]
    [DefaultValue(false)]
    public required bool IsCompleted { get; init; }

    /// <summary>
    /// 仿真开始时间
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public DateTimeOffset? StartTime { get; init; }

    /// <summary>
    /// 仿真结束时间
    /// </summary>
    /// <example>2024-01-15T10:35:00Z</example>
    public DateTimeOffset? EndTime { get; init; }
}

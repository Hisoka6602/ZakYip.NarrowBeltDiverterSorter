using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using ZakYip.NarrowBeltDiverterSorter.Core.Enums;

namespace ZakYip.NarrowBeltDiverterSorter.Host.Contracts.API;

/// <summary>
/// 线体状态响应
/// </summary>
/// <remarks>
/// 返回当前线体的运行状态和安全状态信息
/// </remarks>
/// <example>
/// {
///   "lineRunState": "Running",
///   "safetyState": "Normal",
///   "timestamp": "2024-01-15T10:30:00Z"
/// }
/// </example>
public record class LineStateResponse
{
    /// <summary>
    /// 线体运行状态
    /// </summary>
    /// <remarks>
    /// 可能的值: Stopped (已停止), Starting (启动中), Running (运行中), Stopping (停止中), Paused (已暂停)
    /// </remarks>
    /// <example>Running</example>
    [Required]
    [DefaultValue("Running")]
    public required string LineRunState { get; init; }

    /// <summary>
    /// 安全状态
    /// </summary>
    /// <remarks>
    /// 可能的值: Normal (正常), Warning (警告), Fault (故障), Emergency (紧急停止)
    /// </remarks>
    /// <example>Normal</example>
    [Required]
    [DefaultValue("Normal")]
    public required string SafetyState { get; init; }

    /// <summary>
    /// 时间戳 (UTC)
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    [Required]
    public DateTimeOffset Timestamp { get; init; }
}

/// <summary>
/// 线体操作响应
/// </summary>
/// <remarks>
/// 返回线体操作命令的执行结果和当前状态
/// </remarks>
/// <example>
/// {
///   "success": true,
///   "message": "启动命令已接受",
///   "currentLineRunState": "Starting",
///   "currentSafetyState": "Normal",
///   "timestamp": "2024-01-15T10:30:00Z"
/// }
/// </example>
public record class LineOperationResponse
{
    /// <summary>
    /// 操作是否成功
    /// </summary>
    /// <example>true</example>
    [Required]
    public bool Success { get; init; }

    /// <summary>
    /// 响应消息
    /// </summary>
    /// <remarks>
    /// 提供操作结果的详细说明
    /// </remarks>
    /// <example>启动命令已接受</example>
    [Required]
    [DefaultValue("操作成功")]
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// 当前线体运行状态
    /// </summary>
    /// <remarks>
    /// 可能的值: Stopped, Starting, Running, Stopping, Paused
    /// </remarks>
    /// <example>Starting</example>
    [Required]
    [DefaultValue("Running")]
    public required string CurrentLineRunState { get; init; }

    /// <summary>
    /// 当前安全状态
    /// </summary>
    /// <remarks>
    /// 可能的值: Normal, Warning, Fault, Emergency
    /// </remarks>
    /// <example>Normal</example>
    [Required]
    [DefaultValue("Normal")]
    public required string CurrentSafetyState { get; init; }

    /// <summary>
    /// 时间戳 (UTC)
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    [Required]
    public DateTimeOffset Timestamp { get; init; }
}

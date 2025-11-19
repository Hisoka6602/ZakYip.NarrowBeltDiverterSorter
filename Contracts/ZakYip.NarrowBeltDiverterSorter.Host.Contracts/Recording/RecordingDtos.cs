using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ZakYip.NarrowBeltDiverterSorter.Host.Contracts.Recording;

/// <summary>
/// 启动录制会话请求
/// </summary>
/// <remarks>
/// 用于创建新的事件录制会话
/// </remarks>
/// <example>
/// {
///   "name": "性能测试-20240115",
///   "description": "测试1000包裹高负载场景"
/// }
/// </example>
public class StartRecordingRequest
{
    /// <summary>
    /// 会话名称（必填）
    /// </summary>
    /// <example>性能测试-20240115</example>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public required string Name { get; init; }

    /// <summary>
    /// 会话描述（可选）
    /// </summary>
    /// <example>测试1000包裹高负载场景</example>
    [StringLength(500)]
    public string? Description { get; init; }
}

/// <summary>
/// 录制会话响应
/// </summary>
/// <remarks>
/// 包含录制会话的详细信息和统计数据
/// </remarks>
/// <example>
/// {
///   "sessionId": "123e4567-e89b-12d3-a456-426614174000",
///   "name": "性能测试-20240115",
///   "startedAt": "2024-01-15T10:30:00Z",
///   "stoppedAt": "2024-01-15T10:35:00Z",
///   "description": "测试1000包裹高负载场景",
///   "isCompleted": true,
///   "eventCount": 15234
/// }
/// </example>
public class RecordingSessionResponse
{
    /// <summary>
    /// 会话唯一标识符
    /// </summary>
    /// <example>123e4567-e89b-12d3-a456-426614174000</example>
    [Required]
    public required Guid SessionId { get; init; }

    /// <summary>
    /// 会话名称
    /// </summary>
    /// <example>性能测试-20240115</example>
    [Required]
    public required string Name { get; init; }

    /// <summary>
    /// 会话开始时间 (UTC)
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    [Required]
    public required DateTimeOffset StartedAt { get; init; }

    /// <summary>
    /// 会话停止时间 (UTC)
    /// </summary>
    /// <remarks>
    /// null表示会话仍在进行中
    /// </remarks>
    /// <example>2024-01-15T10:35:00Z</example>
    public DateTimeOffset? StoppedAt { get; init; }

    /// <summary>
    /// 会话描述
    /// </summary>
    /// <example>测试1000包裹高负载场景</example>
    public string? Description { get; init; }

    /// <summary>
    /// 是否正常结束
    /// </summary>
    /// <example>true</example>
    [Required]
    public required bool IsCompleted { get; init; }

    /// <summary>
    /// 已录制的事件数量
    /// </summary>
    /// <example>15234</example>
    [Required]
    public int EventCount { get; init; }
}

/// <summary>
/// 回放模式
/// </summary>
public enum ReplayMode
{
    /// <summary>
    /// 原速回放 - 按照录制时的时间间隔回放
    /// </summary>
    [Description("原速回放")]
    OriginalSpeed,

    /// <summary>
    /// 加速回放 - 按指定倍数加速回放
    /// </summary>
    [Description("加速回放")]
    Accelerated,

    /// <summary>
    /// 固定间隔回放 - 以固定时间间隔回放所有事件
    /// </summary>
    [Description("固定间隔回放")]
    FixedInterval
}

/// <summary>
/// 回放请求
/// </summary>
/// <remarks>
/// 配置录制会话的回放方式和速度
/// </remarks>
/// <example>
/// {
///   "mode": "Accelerated",
///   "speedFactor": 2.0,
///   "fixedIntervalMs": 100
/// }
/// </example>
public class ReplayRequest
{
    /// <summary>
    /// 回放模式
    /// </summary>
    /// <remarks>
    /// OriginalSpeed: 原速回放, Accelerated: 加速回放, FixedInterval: 固定间隔回放
    /// </remarks>
    /// <example>Accelerated</example>
    [Required]
    [DefaultValue(ReplayMode.OriginalSpeed)]
    public ReplayMode Mode { get; init; } = ReplayMode.OriginalSpeed;

    /// <summary>
    /// 加速倍数（仅在加速模式下有效）
    /// </summary>
    /// <remarks>
    /// 例如 2.0 表示以2倍速度回放
    /// </remarks>
    /// <example>2.0</example>
    [Range(0.1, 100)]
    [DefaultValue(1.0)]
    public double SpeedFactor { get; init; } = 1.0;

    /// <summary>
    /// 固定间隔毫秒（仅在固定间隔模式下有效）
    /// </summary>
    /// <example>100</example>
    [Range(1, 10000)]
    [DefaultValue(100)]
    public int FixedIntervalMs { get; init; } = 100;
}

/// <summary>
/// 回放响应
/// </summary>
/// <remarks>
/// 返回回放操作的状态信息
/// </remarks>
/// <example>
/// {
///   "sessionId": "123e4567-e89b-12d3-a456-426614174000",
///   "status": "started",
///   "message": "回放已开始"
/// }
/// </example>
public class ReplayResponse
{
    /// <summary>
    /// 会话唯一标识符
    /// </summary>
    /// <example>123e4567-e89b-12d3-a456-426614174000</example>
    [Required]
    public required Guid SessionId { get; init; }

    /// <summary>
    /// 回放状态
    /// </summary>
    /// <remarks>
    /// 可能的值: started (已开始), running (运行中), completed (已完成), failed (失败)
    /// </remarks>
    /// <example>started</example>
    [Required]
    public required string Status { get; init; }

    /// <summary>
    /// 状态消息
    /// </summary>
    /// <example>回放已开始</example>
    public string? Message { get; init; }
}

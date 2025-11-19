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

namespace ZakYip.NarrowBeltDiverterSorter.UpstreamContracts.Models;

/// <summary>
/// 分拣结果上报DTO
/// 用于向上游系统报告包裹分拣结果（与WheelDiverterSorter接口保持一致）
/// </summary>
public record SortingResultReportDto
{
    /// <summary>
    /// 包裹ID（毫秒时间戳）
    /// </summary>
    public required long ParcelId { get; init; }

    /// <summary>
    /// 目标格口ID（数字ID）
    /// </summary>
    public required int ChuteId { get; init; }

    /// <summary>
    /// 分拣是否成功
    /// </summary>
    public required bool IsSuccess { get; init; }

    /// <summary>
    /// 失败原因（如果失败）
    /// </summary>
    public string? FailureReason { get; init; }

    /// <summary>
    /// 上报时间
    /// </summary>
    public DateTimeOffset ReportTime { get; init; } = DateTimeOffset.UtcNow;
}

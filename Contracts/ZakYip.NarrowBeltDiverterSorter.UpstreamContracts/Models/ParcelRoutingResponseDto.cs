namespace ZakYip.NarrowBeltDiverterSorter.UpstreamContracts.Models;

/// <summary>
/// 包裹路由响应DTO
/// 从上游系统接收的格口分配结果（与WheelDiverterSorter接口保持一致）
/// </summary>
public record ParcelRoutingResponseDto
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
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; init; } = true;

    /// <summary>
    /// 错误消息（如果失败）
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// 响应时间
    /// </summary>
    public DateTimeOffset ResponseTime { get; init; } = DateTimeOffset.UtcNow;
}

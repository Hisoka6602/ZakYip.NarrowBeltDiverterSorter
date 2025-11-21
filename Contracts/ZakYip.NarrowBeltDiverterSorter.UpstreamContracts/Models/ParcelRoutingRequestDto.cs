namespace ZakYip.NarrowBeltDiverterSorter.UpstreamContracts.Models;

/// <summary>
/// 包裹路由请求DTO
/// 用于请求上游系统分配格口（与WheelDiverterSorter接口保持一致）
/// </summary>
public record ParcelRoutingRequestDto
{
    /// <summary>
    /// 包裹ID（毫秒时间戳）
    /// </summary>
    public required long ParcelId { get; init; }

    /// <summary>
    /// 请求时间
    /// </summary>
    public DateTimeOffset RequestTime { get; init; } = DateTimeOffset.Now;
}

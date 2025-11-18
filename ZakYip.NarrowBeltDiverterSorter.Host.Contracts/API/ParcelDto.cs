namespace ZakYip.NarrowBeltDiverterSorter.Host.Contracts.API;

/// <summary>
/// 包裹生命周期 DTO
/// </summary>
public record class ParcelLifecycleDto
{
    public required long ParcelId { get; init; }
    public required string Status { get; init; }
    public required string FailureReason { get; init; }
    public required string RouteState { get; init; }
    public long? TargetChuteId { get; init; }
    public long? ActualChuteId { get; init; }
    public long? BoundCartId { get; init; }
    public long? PredictedCartId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? LoadedAt { get; init; }
    public DateTimeOffset? DivertPlannedAt { get; init; }
    public DateTimeOffset? DivertedAt { get; init; }
    public DateTimeOffset? SortedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public string? SortingOutcome { get; init; }
    public string? DiscardReason { get; init; }
}

/// <summary>
/// 包裹生命周期统计 DTO
/// </summary>
public record class ParcelLifecycleStatsDto
{
    public required Dictionary<string, int> StatusDistribution { get; init; }
    public required Dictionary<string, int> FailureReasonDistribution { get; init; }
    public int OnlineCount { get; init; }
    public int TotalTracked { get; init; }
}

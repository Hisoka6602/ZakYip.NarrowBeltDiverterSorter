namespace ZakYip.NarrowBeltDiverterSorter.Core.SelfCheck;

/// <summary>
/// 小车环健康状态服务接口
/// 跟踪小车环配置的健康状态
/// </summary>
public interface ICartRingHealthService
{
    /// <summary>
    /// 设置小车环配置不匹配状态
    /// </summary>
    /// <param name="expectedCount">期望的小车数量</param>
    /// <param name="detectedCount">实际检测到的小车数量</param>
    void SetCartRingMismatch(int expectedCount, int detectedCount);

    /// <summary>
    /// 清除小车环配置不匹配状态
    /// </summary>
    void ClearCartRingMismatch();

    /// <summary>
    /// 获取当前健康状态
    /// </summary>
    /// <returns>健康状态</returns>
    CartRingHealthStatus GetHealthStatus();
}

/// <summary>
/// 小车环健康状态
/// </summary>
public sealed record CartRingHealthStatus
{
    /// <summary>
    /// 是否健康
    /// </summary>
    public bool IsHealthy { get; init; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// 期望的小车数量（如果有不匹配）
    /// </summary>
    public int? ExpectedCartCount { get; init; }

    /// <summary>
    /// 实际检测到的小车数量（如果有不匹配）
    /// </summary>
    public int? DetectedCartCount { get; init; }

    /// <summary>
    /// 创建健康状态
    /// </summary>
    public static CartRingHealthStatus Healthy()
    {
        return new CartRingHealthStatus { IsHealthy = true };
    }

    /// <summary>
    /// 创建不匹配状态
    /// </summary>
    public static CartRingHealthStatus Mismatch(int expectedCount, int detectedCount)
    {
        return new CartRingHealthStatus
        {
            IsHealthy = false,
            ErrorMessage = $"小车环配置不匹配：期望 {expectedCount} 辆，实际检测到 {detectedCount} 辆",
            ExpectedCartCount = expectedCount,
            DetectedCartCount = detectedCount
        };
    }
}

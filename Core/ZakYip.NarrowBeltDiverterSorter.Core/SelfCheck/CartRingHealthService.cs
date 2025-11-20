namespace ZakYip.NarrowBeltDiverterSorter.Core.SelfCheck;

/// <summary>
/// 小车环健康状态服务实现
/// 跟踪小车环配置的健康状态
/// </summary>
public sealed class CartRingHealthService : ICartRingHealthService
{
    private readonly object _lock = new();
    private CartRingHealthStatus _currentStatus = CartRingHealthStatus.Healthy();

    /// <inheritdoc/>
    public void SetCartRingMismatch(int expectedCount, int detectedCount)
    {
        lock (_lock)
        {
            _currentStatus = CartRingHealthStatus.Mismatch(expectedCount, detectedCount);
        }
    }

    /// <inheritdoc/>
    public void ClearCartRingMismatch()
    {
        lock (_lock)
        {
            _currentStatus = CartRingHealthStatus.Healthy();
        }
    }

    /// <inheritdoc/>
    public CartRingHealthStatus GetHealthStatus()
    {
        lock (_lock)
        {
            return _currentStatus;
        }
    }
}

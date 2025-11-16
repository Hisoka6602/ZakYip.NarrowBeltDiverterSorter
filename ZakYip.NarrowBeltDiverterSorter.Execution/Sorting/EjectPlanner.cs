using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Carts;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.MainLine;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Sorting;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Tracking;

namespace ZakYip.NarrowBeltDiverterSorter.Execution.Sorting;

/// <summary>
/// 吐件规划器实现
/// 根据小车位置跟踪和主线速度，计算小车到达格口的时间窗口
/// </summary>
public class EjectPlanner : IEjectPlanner
{
    private readonly ICartRingBuilder _cartRingBuilder;
    private readonly ICartPositionTracker _cartPositionTracker;
    private readonly IMainLineSpeedProvider _mainLineSpeedProvider;
    private readonly IMainLineStabilityProvider _stabilityProvider;
    private readonly IChuteConfigProvider _chuteConfigProvider;
    private readonly SortingPlannerOptions _options;

    public EjectPlanner(
        ICartRingBuilder cartRingBuilder,
        ICartPositionTracker cartPositionTracker,
        IMainLineSpeedProvider mainLineSpeedProvider,
        IMainLineStabilityProvider stabilityProvider,
        IChuteConfigProvider chuteConfigProvider,
        SortingPlannerOptions options)
    {
        _cartRingBuilder = cartRingBuilder;
        _cartPositionTracker = cartPositionTracker;
        _mainLineSpeedProvider = mainLineSpeedProvider;
        _stabilityProvider = stabilityProvider;
        _chuteConfigProvider = chuteConfigProvider;
        _options = options;
    }

    /// <inheritdoc/>
    public DivertPlan? CalculateDivertPlan(CartId cartId, ChuteId chuteId, DateTimeOffset now)
    {
        // Check if cart ring is ready
        var cartRing = _cartRingBuilder.CurrentSnapshot;
        if (cartRing == null || !_cartPositionTracker.IsRingReady)
        {
            return null;
        }

        // Check if main line speed is stable
        // Use the stability provider to determine if conditions are suitable for ejecting
        if (!_stabilityProvider.IsStable)
        {
            return null;
        }

        var currentSpeed = _mainLineSpeedProvider.CurrentMmps;
        if (currentSpeed <= 0)
        {
            return null;
        }

        // Get chute configuration
        var chuteConfig = _chuteConfigProvider.GetConfig(chuteId);
        if (chuteConfig == null || !chuteConfig.IsEnabled)
        {
            return null;
        }

        // Find cart index from cart ID
        int? cartIndex = null;
        for (int i = 0; i < cartRing.RingLength.Value; i++)
        {
            if (cartRing.CartIds[i].Value == cartId.Value)
            {
                cartIndex = i;
                break;
            }
        }

        if (!cartIndex.HasValue)
        {
            return null;
        }

        // Calculate current position of the cart
        var currentOriginIndex = _cartPositionTracker.CurrentOriginCartIndex;
        if (currentOriginIndex == null)
        {
            return null;
        }

        // Calculate distance from current position to chute
        int offsetFromCurrentOrigin = (cartIndex.Value - currentOriginIndex.Value.Value + cartRing.RingLength.Value) % cartRing.RingLength.Value;
        decimal distanceToChuteMm = offsetFromCurrentOrigin * _options.CartSpacingMm;

        // Adjust for the chute's cart offset from origin
        distanceToChuteMm -= chuteConfig.CartOffsetFromOrigin * _options.CartSpacingMm;

        // Handle wrap-around
        if (distanceToChuteMm < 0)
        {
            distanceToChuteMm += cartRing.RingLength.Value * _options.CartSpacingMm;
        }

        // Calculate time to reach chute
        var timeToChuteSec = (double)distanceToChuteMm / (double)currentSpeed;
        var windowStart = now.AddSeconds(timeToChuteSec);

        // Calculate window duration (one cart spacing worth of time)
        var windowDurationSec = (double)_options.CartSpacingMm / (double)currentSpeed;
        var windowEnd = windowStart.AddSeconds(windowDurationSec);

        return new DivertPlan
        {
            CartId = cartId,
            ChuteId = chuteId,
            WindowStart = windowStart,
            WindowEnd = windowEnd
        };
    }
}

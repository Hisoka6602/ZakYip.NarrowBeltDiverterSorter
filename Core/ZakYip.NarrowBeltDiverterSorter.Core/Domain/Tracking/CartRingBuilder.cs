namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.Tracking;

/// <summary>
/// 小车环构建器实现
/// </summary>
public class CartRingBuilder : ICartRingBuilder
{
    private enum BuildState
    {
        Building,
        Completed,
        Invalid
    }

    private BuildState _state = BuildState.Building;
    private bool _sensor1Blocked = false;
    private bool _sensor2Blocked = false;
    private bool _bothSensorsWereBlocked = false; // Track if both sensors were blocked during current cart passage
    private int _cartCount = 0;
    private bool _firstZeroCartPassed = false;
    private DateTimeOffset? _firstZeroCartTime;
    private readonly List<CartId> _cartIds = new();

    /// <inheritdoc/>
    public CartRingSnapshot? CurrentSnapshot { get; private set; }

    /// <inheritdoc/>
    public void OnOriginSensorTriggered(bool isFirstSensor, bool isRisingEdge, DateTimeOffset timestamp)
    {
        if (_state != BuildState.Building)
        {
            return;
        }

        // Update sensor state
        if (isFirstSensor)
        {
            _sensor1Blocked = isRisingEdge;
        }
        else
        {
            _sensor2Blocked = isRisingEdge;
        }

        // Check if both sensors are currently blocked
        if (_sensor1Blocked && _sensor2Blocked)
        {
            _bothSensorsWereBlocked = true;
        }

        // Detect cart passage completion - when both sensors are unblocked after a cart passed
        if (!_sensor1Blocked && !_sensor2Blocked && _bothSensorsWereBlocked)
        {
            // A cart has completely passed
            bool wasZeroCart = _bothSensorsWereBlocked;

            if (wasZeroCart)
            {
                if (!_firstZeroCartPassed)
                {
                    // First zero cart detection - start counting
                    _firstZeroCartPassed = true;
                    _firstZeroCartTime = timestamp;
                    _cartCount = 1;
                    var cartId = new CartId(0);
                    _cartIds.Add(cartId); // Zero cart ID is 0
                    RaiseCartPassed(cartId, timestamp);
                }
                else
                {
                    // Second zero cart detection - complete the ring
                    CompleteRing(timestamp);
                    RaiseCartPassed(new CartId(0), timestamp);
                }
            }

            // Reset for next cart
            _bothSensorsWereBlocked = false;
        }
        // Detect regular cart passage - when sensor 1 goes from blocked to unblocked without sensor 2 being blocked
        else if (!isRisingEdge && isFirstSensor && !_sensor2Blocked && !_bothSensorsWereBlocked && _firstZeroCartPassed)
        {
            // Regular cart has passed (only sensor 1 was blocked)
            _cartCount++;
            var cartId = new CartId(_cartCount - 1);
            _cartIds.Add(cartId);
            RaiseCartPassed(cartId, timestamp);
        }
    }

    private void CompleteRing(DateTimeOffset timestamp)
    {
        if (_cartCount <= 0)
        {
            _state = BuildState.Invalid;
            return;
        }

        CurrentSnapshot = new CartRingSnapshot
        {
            RingLength = new RingLength(_cartCount),
            ZeroCartId = new CartId(0),
            ZeroIndex = new CartIndex(0),
            CartIds = _cartIds.AsReadOnly(),
            BuiltAt = timestamp
        };

        _state = BuildState.Completed;
    }

    private void RaiseCartPassed(CartId cartId, DateTimeOffset timestamp)
    {
        // TD-IMPL-006: CartRingBuilder事件发布未实现
        // 需要注入 IEventBus 并发布 CartPassedEventArgs 事件
        // 当前该方法被调用但无实际效果
    }
}

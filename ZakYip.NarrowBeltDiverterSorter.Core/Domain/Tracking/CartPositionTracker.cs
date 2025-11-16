namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.Tracking;

/// <summary>
/// 小车位置跟踪器实现
/// </summary>
public class CartPositionTracker : ICartPositionTracker
{
    private readonly ICartRingBuilder _cartRingBuilder;
    private CartIndex? _currentOriginCartIndex;
    private RingLength? _ringLength;
    private bool _isInitialized;
    private bool _isRingReady;

    public CartPositionTracker(ICartRingBuilder cartRingBuilder)
    {
        _cartRingBuilder = cartRingBuilder ?? throw new ArgumentNullException(nameof(cartRingBuilder));
    }

    /// <inheritdoc/>
    public bool IsInitialized => _isInitialized;

    /// <inheritdoc/>
    public bool IsRingReady => _isRingReady;

    /// <inheritdoc/>
    public CartIndex? CurrentOriginCartIndex => _currentOriginCartIndex;

    /// <inheritdoc/>
    public void OnCartPassedOrigin(DateTimeOffset timestamp)
    {
        // Check if cart ring is built
        var snapshot = _cartRingBuilder.CurrentSnapshot;
        if (snapshot == null)
        {
            // Cart ring not yet built, cannot track
            return;
        }

        if (!_isInitialized)
        {
            // First cart detection after ring is built - initialize with zero cart
            _currentOriginCartIndex = snapshot.ZeroIndex;
            _ringLength = snapshot.RingLength;
            _isInitialized = true;
            // Mark ring as ready: cart ring is built and first cart has passed origin
            _isRingReady = true;
        }
        else
        {
            // Increment to next cart, wrapping around the ring
            var nextIndex = (_currentOriginCartIndex!.Value.Value + 1) % _ringLength!.Value.Value;
            _currentOriginCartIndex = new CartIndex(nextIndex);
        }
    }

    /// <inheritdoc/>
    public CartIndex? CalculateCartIndexAtOffset(int offset, RingLength ringLength)
    {
        if (!_isInitialized || _currentOriginCartIndex == null || ringLength.Value <= 0)
        {
            return null;
        }

        // Calculate the cart index at the given offset, wrapping around the ring
        var calculatedIndex = (_currentOriginCartIndex.Value.Value + offset) % ringLength.Value;
        if (calculatedIndex < 0)
        {
            calculatedIndex += ringLength.Value;
        }

        return new CartIndex(calculatedIndex);
    }
}

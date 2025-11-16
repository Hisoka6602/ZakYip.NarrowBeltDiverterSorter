namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.Tracking;

/// <summary>
/// 小车位置跟踪器实现
/// </summary>
public class CartPositionTracker : ICartPositionTracker
{
    private CartIndex? _currentOriginCartIndex;
    private bool _isInitialized;

    /// <inheritdoc/>
    public bool IsInitialized => _isInitialized;

    /// <inheritdoc/>
    public CartIndex? CurrentOriginCartIndex => _currentOriginCartIndex;

    /// <inheritdoc/>
    public void OnCartPassedOrigin(DateTimeOffset timestamp)
    {
        if (_currentOriginCartIndex == null)
        {
            // First cart detection - assume it's cart 0
            _currentOriginCartIndex = new CartIndex(0);
            _isInitialized = true; // Mark as initialized on first cart detection
        }
        else
        {
            // Increment to next cart (will wrap around in CalculateCartIndexAtOffset)
            _currentOriginCartIndex = new CartIndex(_currentOriginCartIndex.Value.Value + 1);
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

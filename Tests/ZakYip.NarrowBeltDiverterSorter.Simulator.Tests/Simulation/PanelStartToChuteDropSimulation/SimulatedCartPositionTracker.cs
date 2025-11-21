using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Tracking;

namespace ZakYip.NarrowBeltDiverterSorter.Simulator.Tests.Simulation.PanelStartToChuteDropSimulation;

/// <summary>
/// 仿真小车位置跟踪器
/// 用于在仿真环境中跟踪小车位置，基于仿真时钟驱动
/// </summary>
public sealed class SimulatedCartPositionTracker : ICartPositionTracker
{
    private readonly int _totalCartCount;
    private int _currentOriginCartIndex;
    private bool _isInitialized;
    private bool _isRingReady;

    public SimulatedCartPositionTracker(int totalCartCount)
    {
        _totalCartCount = totalCartCount;
        _currentOriginCartIndex = 0; // 从0号车开始（0-based index）
        _isInitialized = false;
        _isRingReady = false;
    }

    /// <inheritdoc/>
    public bool IsInitialized => _isInitialized;

    /// <inheritdoc/>
    public bool IsRingReady => _isRingReady;

    /// <inheritdoc/>
    public CartIndex? CurrentOriginCartIndex => _isInitialized 
        ? new CartIndex(_currentOriginCartIndex) 
        : null;

    /// <summary>
    /// 初始化跟踪器（仿真专用）
    /// </summary>
    public void Initialize()
    {
        _isInitialized = true;
        _isRingReady = true;
    }

    /// <summary>
    /// 设置当前原点小车索引（仿真专用）
    /// </summary>
    /// <param name="cartIndex">小车索引（0-based）</param>
    public void SetCurrentOriginCartIndex(int cartIndex)
    {
        if (cartIndex < 0 || cartIndex >= _totalCartCount)
        {
            throw new ArgumentOutOfRangeException(nameof(cartIndex), 
                $"Cart index must be between 0 and {_totalCartCount - 1}");
        }

        _currentOriginCartIndex = cartIndex;
    }

    /// <inheritdoc/>
    public void OnCartPassedOrigin(DateTimeOffset timestamp)
    {
        // 小车经过原点，索引前进
        _currentOriginCartIndex = (_currentOriginCartIndex + 1) % _totalCartCount;
    }

    /// <inheritdoc/>
    public CartIndex? CalculateCartIndexAtOffset(int offset, RingLength ringLength)
    {
        if (!_isInitialized)
        {
            return null;
        }

        var cartIndex = (_currentOriginCartIndex + offset) % _totalCartCount;
        if (cartIndex < 0)
        {
            cartIndex += _totalCartCount;
        }

        return new CartIndex(cartIndex);
    }
}

using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.MainLine;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Tracking;
using ZakYip.NarrowBeltDiverterSorter.Core.SelfCheck;

namespace ZakYip.NarrowBeltDiverterSorter.Simulation.SelfCheck;

/// <summary>
/// 小车自检事件收集器
/// 从原点传感器事件中收集小车通过信息，用于自检分析
/// </summary>
public class CartSelfCheckEventCollector
{
    private readonly ILogger<CartSelfCheckEventCollector> _logger;
    private readonly IMainLineSpeedProvider _speedProvider;
    private readonly ICartRingBuilder _cartRingBuilder;
    private readonly List<CartPassEventArgs> _collectedEvents = new();
    private readonly object _lock = new();

    public CartSelfCheckEventCollector(
        ILogger<CartSelfCheckEventCollector> logger,
        IMainLineSpeedProvider speedProvider,
        ICartRingBuilder cartRingBuilder)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _speedProvider = speedProvider ?? throw new ArgumentNullException(nameof(speedProvider));
        _cartRingBuilder = cartRingBuilder ?? throw new ArgumentNullException(nameof(cartRingBuilder));
    }

    /// <summary>
    /// 开始收集事件
    /// </summary>
    public void StartCollecting()
    {
        lock (_lock)
        {
            _collectedEvents.Clear();
        }
        
        _cartRingBuilder.OnCartPassed += OnCartPassed;
        _logger.LogInformation("开始收集小车通过事件");
    }

    /// <summary>
    /// 停止收集事件
    /// </summary>
    public void StopCollecting()
    {
        _cartRingBuilder.OnCartPassed -= OnCartPassed;
        _logger.LogInformation("停止收集小车通过事件，共收集 {Count} 个事件", GetCollectedEvents().Count);
    }

    /// <summary>
    /// 获取收集到的事件列表
    /// </summary>
    public IReadOnlyList<CartPassEventArgs> GetCollectedEvents()
    {
        lock (_lock)
        {
            return _collectedEvents.ToList();
        }
    }

    /// <summary>
    /// 处理小车通过事件
    /// </summary>
    private void OnCartPassed(object? sender, CartPassedEventArgs e)
    {
        var currentSpeed = _speedProvider.CurrentMmps;
        
        var passEvent = new CartPassEventArgs
        {
            CartId = (int)e.CartId.Value,
            PassAt = e.PassAt,
            LineSpeedMmps = currentSpeed
        };

        lock (_lock)
        {
            _collectedEvents.Add(passEvent);
        }

        _logger.LogDebug(
            "收集到小车通过事件 - CartId: {CartId}, Speed: {Speed:F2} mm/s",
            passEvent.CartId,
            passEvent.LineSpeedMmps);
    }
}

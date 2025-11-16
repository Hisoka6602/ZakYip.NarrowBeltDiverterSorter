using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Carts;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Feeding;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Tracking;

namespace ZakYip.NarrowBeltDiverterSorter.Execution.Feeding;

/// <summary>
/// 包裹装载计划器实现
/// 基于入口时间、输送线速度和小车环状态预测包裹将装载到哪个小车
/// </summary>
public class ParcelLoadPlanner : IParcelLoadPlanner
{
    private readonly ICartRingBuilder _cartRingBuilder;
    private readonly ICartPositionTracker _cartPositionTracker;
    private readonly IMainLineFeedbackPort _mainLineFeedback;
    private readonly IInfeedConveyorPort _infeedConveyor;
    private readonly ICartLifecycleService _cartLifecycleService;
    private readonly InfeedLayoutOptions _options;

    /// <summary>
    /// 创建包裹装载计划器
    /// </summary>
    public ParcelLoadPlanner(
        ICartRingBuilder cartRingBuilder,
        ICartPositionTracker cartPositionTracker,
        IMainLineFeedbackPort mainLineFeedback,
        IInfeedConveyorPort infeedConveyor,
        ICartLifecycleService cartLifecycleService,
        InfeedLayoutOptions options)
    {
        _cartRingBuilder = cartRingBuilder ?? throw new ArgumentNullException(nameof(cartRingBuilder));
        _cartPositionTracker = cartPositionTracker ?? throw new ArgumentNullException(nameof(cartPositionTracker));
        _mainLineFeedback = mainLineFeedback ?? throw new ArgumentNullException(nameof(mainLineFeedback));
        _infeedConveyor = infeedConveyor ?? throw new ArgumentNullException(nameof(infeedConveyor));
        _cartLifecycleService = cartLifecycleService ?? throw new ArgumentNullException(nameof(cartLifecycleService));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc/>
    public Task<CartId?> PredictLoadedCartAsync(DateTimeOffset infeedEdgeTime, CancellationToken ct)
    {
        // 检查小车环是否已就绪
        if (!_cartPositionTracker.IsRingReady)
        {
            return Task.FromResult<CartId?>(null);
        }

        // 获取小车环快照
        var snapshot = _cartRingBuilder.CurrentSnapshot;
        if (snapshot == null)
        {
            return Task.FromResult<CartId?>(null);
        }

        // 获取入口输送线速度
        var infeedSpeed = _infeedConveyor.GetCurrentSpeed();
        if (infeedSpeed <= 0)
        {
            return Task.FromResult<CartId?>(null);
        }

        // 计算包裹从入口到主线落车点的时间
        var travelTimeSeconds = (double)_options.InfeedToMainLineDistanceMm / infeedSpeed;

        // 获取主线速度
        var mainLineSpeed = _mainLineFeedback.GetCurrentSpeed();
        if (mainLineSpeed <= 0)
        {
            return Task.FromResult<CartId?>(null);
        }

        // 使用 CartPositionTracker 计算当前在落车点位置（CartOffsetCalibration）的小车
        var cartIndexAtDropPoint = _cartPositionTracker.CalculateCartIndexAtOffset(
            _options.CartOffsetCalibration, 
            snapshot.RingLength);

        if (cartIndexAtDropPoint == null)
        {
            return Task.FromResult<CartId?>(null);
        }

        // 查找第一个未装载的小车（一个小车只能承载一个包裹）
        // 从预测的落车点开始，向前搜索整个环
        var ringLength = snapshot.RingLength.Value;
        for (int offset = 0; offset < ringLength; offset++)
        {
            var cartIndex = (cartIndexAtDropPoint.Value.Value + offset) % ringLength;
            var candidateCartId = snapshot.CartIds[cartIndex];
            
            // 检查该小车是否已经装载包裹
            var cartSnapshot = _cartLifecycleService.Get(candidateCartId);
            if (cartSnapshot != null && !cartSnapshot.IsLoaded)
            {
                // 找到未装载的小车
                return Task.FromResult<CartId?>(candidateCartId);
            }
        }

        // 如果整个环的所有小车都已装载，返回null
        return Task.FromResult<CartId?>(null);
    }
}

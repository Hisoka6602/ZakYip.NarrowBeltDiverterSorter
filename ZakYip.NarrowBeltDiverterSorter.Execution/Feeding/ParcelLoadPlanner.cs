using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
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
    private readonly IMainLineFeedbackPort _mainLineFeedback;
    private readonly IInfeedConveyorPort _infeedConveyor;
    private readonly InfeedLayoutOptions _options;

    /// <summary>
    /// 创建包裹装载计划器
    /// </summary>
    public ParcelLoadPlanner(
        ICartRingBuilder cartRingBuilder,
        IMainLineFeedbackPort mainLineFeedback,
        IInfeedConveyorPort infeedConveyor,
        InfeedLayoutOptions options)
    {
        _cartRingBuilder = cartRingBuilder ?? throw new ArgumentNullException(nameof(cartRingBuilder));
        _mainLineFeedback = mainLineFeedback ?? throw new ArgumentNullException(nameof(mainLineFeedback));
        _infeedConveyor = infeedConveyor ?? throw new ArgumentNullException(nameof(infeedConveyor));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc/>
    public Task<CartId?> PredictLoadedCartAsync(DateTimeOffset infeedEdgeTime, CancellationToken ct)
    {
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
        var arrivalTime = infeedEdgeTime.AddSeconds(travelTimeSeconds);

        // 获取主线速度
        var mainLineSpeed = _mainLineFeedback.GetCurrentSpeed();
        if (mainLineSpeed <= 0)
        {
            return Task.FromResult<CartId?>(null);
        }

        // 计算从当前时间到到达时间，主线移动的距离
        var timeDiff = (arrivalTime - DateTimeOffset.UtcNow).TotalSeconds;
        var distanceMoved = mainLineSpeed * timeDiff;

        // 估算小车环的周长（小车数量 * 小车间距）
        // 注意：这是一个简化实现，实际应该从小车环配置中获取
        var ringLength = snapshot.RingLength.Value;
        
        // 简化实现：假设当前零号小车在原点，根据移动距离和小车间距计算到达时哪个小车在落车点
        // 这个实现需要根据实际系统的小车环配置和传感器位置进行调整
        
        // 应用偏移校准
        var predictedCartIndex = _options.CartOffsetCalibration % ringLength;
        var predictedCartId = new CartId(predictedCartIndex);

        return Task.FromResult<CartId?>(predictedCartId);
    }
}

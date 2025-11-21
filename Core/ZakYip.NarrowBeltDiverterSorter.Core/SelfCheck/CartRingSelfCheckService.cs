using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Tracking;

namespace ZakYip.NarrowBeltDiverterSorter.Core.SelfCheck;

/// <summary>
/// 小车环自检服务实现
/// 基于时间间隔分析小车通过事件，统计小车数量和节距
/// </summary>
public class CartRingSelfCheckService : ICartRingSelfCheckService
{
    private readonly CartRingSelfCheckOptions _options;

    public CartRingSelfCheckService(CartRingSelfCheckOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc/>
    public CartRingSelfCheckResult RunAnalysis(
        IReadOnlyList<CartPassEventArgs> passEvents,
        TrackTopologySnapshot topologySnapshot)
    {
        if (passEvents == null)
            throw new ArgumentNullException(nameof(passEvents));
        
        if (topologySnapshot == null)
            throw new ArgumentNullException(nameof(topologySnapshot));

        if (passEvents.Count == 0)
        {
            return CreateEmptyResult(topologySnapshot);
        }

        // 统计不同的小车ID数量
        var uniqueCartIds = new HashSet<int>();
        foreach (var evt in passEvents)
        {
            uniqueCartIds.Add(evt.CartId);
        }
        int measuredCartCount = uniqueCartIds.Count;

        // 计算平均节距
        decimal measuredPitchMm = CalculateAveragePitch(passEvents);

        // 使用 TotalCartCount 如果 > 0，否则使用 CartCount（向后兼容）
        int expectedCartCount = topologySnapshot.TotalCartCount > 0 
            ? topologySnapshot.TotalCartCount 
            : topologySnapshot.CartCount;

        // 判断小车数量是否匹配
        bool isCartCountMatched = measuredCartCount == expectedCartCount;

        // 判断节距是否在容忍范围内
        bool isPitchWithinTolerance = IsPitchWithinTolerance(
            measuredPitchMm,
            topologySnapshot.CartSpacingMm);

        return new CartRingSelfCheckResult
        {
            ExpectedCartCount = expectedCartCount,
            MeasuredCartCount = measuredCartCount,
            ExpectedPitchMm = topologySnapshot.CartSpacingMm,
            MeasuredPitchMm = measuredPitchMm,
            IsCartCountMatched = isCartCountMatched,
            IsPitchWithinTolerance = isPitchWithinTolerance
        };
    }

    /// <summary>
    /// 创建空结果（当没有事件数据时）
    /// </summary>
    private CartRingSelfCheckResult CreateEmptyResult(TrackTopologySnapshot topologySnapshot)
    {
        // 使用 TotalCartCount 如果 > 0，否则使用 CartCount（向后兼容）
        int expectedCartCount = topologySnapshot.TotalCartCount > 0 
            ? topologySnapshot.TotalCartCount 
            : topologySnapshot.CartCount;

        return new CartRingSelfCheckResult
        {
            ExpectedCartCount = expectedCartCount,
            MeasuredCartCount = 0,
            ExpectedPitchMm = topologySnapshot.CartSpacingMm,
            MeasuredPitchMm = 0m,
            IsCartCountMatched = false,
            IsPitchWithinTolerance = false
        };
    }

    /// <summary>
    /// 计算平均节距
    /// 基于相邻小车通过的时间差和速度计算
    /// </summary>
    private decimal CalculateAveragePitch(IReadOnlyList<CartPassEventArgs> passEvents)
    {
        if (passEvents.Count < 2)
            return 0m;

        var pitchSamples = new List<decimal>();

        // 计算相邻事件之间的节距
        for (int i = 1; i < passEvents.Count; i++)
        {
            var prev = passEvents[i - 1];
            var curr = passEvents[i];

            // 时间差（秒）
            var timeDiffSeconds = (decimal)(curr.PassAt - prev.PassAt).TotalSeconds;

            // 只考虑合理的时间差（避免异常值）
            if (timeDiffSeconds <= 0 || timeDiffSeconds > 10)
                continue;

            // 平均速度
            var avgSpeed = (prev.LineSpeedMmps + curr.LineSpeedMmps) / 2m;

            if (avgSpeed <= 0)
                continue;

            // 节距 = 速度 × 时间
            var pitch = avgSpeed * timeDiffSeconds;

            // 过滤明显异常的节距值（应该在合理范围内）
            if (pitch > 0 && pitch < 10000) // 假设节距不会超过10米
            {
                pitchSamples.Add(pitch);
            }
        }

        if (pitchSamples.Count == 0)
            return 0m;

        // 返回中位数而不是平均值，以减少异常值的影响
        return CalculateMedian(pitchSamples);
    }

    /// <summary>
    /// 计算中位数
    /// </summary>
    private decimal CalculateMedian(List<decimal> values)
    {
        if (values.Count == 0)
            return 0m;

        var sorted = values.OrderBy(x => x).ToList();
        int mid = sorted.Count / 2;

        if (sorted.Count % 2 == 0)
        {
            return (sorted[mid - 1] + sorted[mid]) / 2m;
        }
        else
        {
            return sorted[mid];
        }
    }

    /// <summary>
    /// 判断节距是否在容忍范围内
    /// </summary>
    private bool IsPitchWithinTolerance(decimal measuredPitch, decimal expectedPitch)
    {
        if (expectedPitch <= 0)
            return false;

        if (measuredPitch <= 0)
            return false;

        var deviation = Math.Abs(measuredPitch - expectedPitch);
        var deviationPercent = deviation / expectedPitch;

        return deviationPercent <= (decimal)_options.PitchTolerancePercent;
    }
}

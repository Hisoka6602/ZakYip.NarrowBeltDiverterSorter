using ZakYip.NarrowBeltDiverterSorter.Core.Domain.MainLine;

namespace ZakYip.NarrowBeltDiverterSorter.Execution.Mainline;

/// <summary>
/// 主线稳定性提供者默认实现
/// 基于 IMainLineSpeedProvider 的稳定性判断
/// </summary>
public class MainLineStabilityProvider : IMainLineStabilityProvider
{
    private readonly IMainLineSpeedProvider _speedProvider;

    public MainLineStabilityProvider(IMainLineSpeedProvider speedProvider)
    {
        _speedProvider = speedProvider;
    }

    /// <inheritdoc/>
    public bool IsStable => _speedProvider.IsSpeedStable;

    /// <inheritdoc/>
    public bool IsStableForWindow(DateTimeOffset windowStart, TimeSpan windowDuration)
    {
        // 简化实现：如果当前速度稳定，则认为整个窗口内速度都稳定
        // 在实际应用中，可以根据历史速度数据进行更精确的预测
        return _speedProvider.IsSpeedStable;
    }
}

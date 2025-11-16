namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.MainLine;

/// <summary>
/// 主线稳定性提供者接口
/// 用于判断主线速度是否处于允许的稳定范围内，适合执行吐件操作
/// </summary>
public interface IMainLineStabilityProvider
{
    /// <summary>
    /// 当前主线速度是否处于允许的稳定范围内
    /// </summary>
    bool IsStable { get; }

    /// <summary>
    /// 在指定时间窗口内是否可以认为主线速度足够稳定，适合执行吐件
    /// </summary>
    /// <param name="windowStart">窗口开始时间</param>
    /// <param name="windowDuration">窗口持续时间</param>
    /// <returns>如果窗口内速度稳定则返回 true，否则返回 false</returns>
    bool IsStableForWindow(DateTimeOffset windowStart, TimeSpan windowDuration);
}

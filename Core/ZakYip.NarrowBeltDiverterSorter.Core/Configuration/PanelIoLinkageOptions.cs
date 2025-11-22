namespace ZakYip.NarrowBeltDiverterSorter.Core.Configuration;

/// <summary>
/// 面板 IO 联动配置选项
/// 定义跟随启动/停止/首次稳速/稳速后不稳速的输出通道列表
/// </summary>
public sealed record class PanelIoLinkageOptions
{
    /// <summary>
    /// 跟随启动的输出通道列表
    /// 当系统启动时，这些通道将被设置为ON(1)
    /// </summary>
    public IReadOnlyList<int> StartFollowOutputChannels { get; init; } = Array.Empty<int>();

    /// <summary>
    /// 跟随停止的输出通道列表
    /// 当系统停止或急停时，这些通道将被设置为OFF(0)
    /// </summary>
    public IReadOnlyList<int> StopFollowOutputChannels { get; init; } = Array.Empty<int>();

    /// <summary>
    /// 首次稳速时联动的输出通道列表
    /// 在一次启动→运行周期中，当线体速度首次进入稳速状态时，这些通道将被设置为ON(1)
    /// 只触发一次，直到线体停止/重新启动
    /// </summary>
    public IReadOnlyList<int> FirstStableSpeedFollowOutputChannels { get; init; } = Array.Empty<int>();

    /// <summary>
    /// 稳速后的每次不稳速时联动的输出通道列表
    /// 前提：本次运行中线体已至少有一次进入过稳速状态
    /// 每次从稳速状态退化为不稳速状态时，这些通道将被设置为ON(1)
    /// </summary>
    public IReadOnlyList<int> UnstableAfterStableFollowOutputChannels { get; init; } = Array.Empty<int>();
}

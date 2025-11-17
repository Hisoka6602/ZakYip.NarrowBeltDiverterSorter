namespace ZakYip.NarrowBeltDiverterSorter.Execution.Panel;

/// <summary>
/// 面板 IO 联动配置选项
/// 定义跟随启动/停止的输出通道列表
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
}

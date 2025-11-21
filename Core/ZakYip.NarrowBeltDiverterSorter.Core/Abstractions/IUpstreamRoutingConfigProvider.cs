using ZakYip.NarrowBeltDiverterSorter.Core.Configuration;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;

/// <summary>
/// 上游路由配置提供器接口
/// 提供运行时上游路由配置的访问和变更通知
/// </summary>
public interface IUpstreamRoutingConfigProvider
{
    /// <summary>
    /// 获取当前的上游路由配置
    /// </summary>
    /// <returns>上游路由配置选项</returns>
    UpstreamRoutingOptions GetCurrentOptions();

    /// <summary>
    /// 更新上游路由配置
    /// </summary>
    /// <param name="newOptions">新的配置选项</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task UpdateOptionsAsync(UpstreamRoutingOptions newOptions, CancellationToken cancellationToken = default);

    /// <summary>
    /// 配置变更事件
    /// 当配置被更新时触发
    /// </summary>
    event EventHandler<UpstreamRoutingConfigChangedEventArgs>? ConfigChanged;
}

/// <summary>
/// 上游路由配置变更事件参数
/// </summary>
public class UpstreamRoutingConfigChangedEventArgs : EventArgs
{
    /// <summary>
    /// 新的配置选项
    /// </summary>
    public required UpstreamRoutingOptions NewOptions { get; init; }

    /// <summary>
    /// 变更时间（本地时间）
    /// </summary>
    public DateTimeOffset ChangedAt { get; init; } = DateTimeOffset.Now;
}

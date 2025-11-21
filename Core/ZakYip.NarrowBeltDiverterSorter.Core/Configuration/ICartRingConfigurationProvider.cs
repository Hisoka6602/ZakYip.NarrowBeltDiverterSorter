namespace ZakYip.NarrowBeltDiverterSorter.Core.Configuration;

/// <summary>
/// 小车环配置提供器接口
/// 负责提供小车环配置，支持运行期热更新
/// </summary>
public interface ICartRingConfigurationProvider
{
    /// <summary>
    /// 获取当前小车环配置快照
    /// </summary>
    CartRingConfiguration Current { get; }

    /// <summary>
    /// 异步更新小车环配置
    /// </summary>
    /// <param name="configuration">新的配置</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task UpdateAsync(CartRingConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步加载小车环配置
    /// 如果存储中不存在配置，则使用默认值并初始化
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    Task<CartRingConfiguration> LoadAsync(CancellationToken cancellationToken = default);
}

namespace ZakYip.NarrowBeltDiverterSorter.Core.Configuration;

/// <summary>
/// Sorter 配置提供器接口
/// 负责从 LiteDB 加载和更新 Sorter 配置，支持热更新
/// </summary>
public interface ISorterConfigurationProvider
{
    /// <summary>
    /// 获取当前 Sorter 配置
    /// </summary>
    SorterOptions Current { get; }

    /// <summary>
    /// 异步更新 Sorter 配置
    /// </summary>
    /// <param name="options">新的配置选项</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task UpdateAsync(SorterOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步加载 Sorter 配置
    /// 如果 LiteDB 中不存在配置，则从 appsettings.json 读取默认值并初始化
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    Task<SorterOptions> LoadAsync(CancellationToken cancellationToken = default);
}

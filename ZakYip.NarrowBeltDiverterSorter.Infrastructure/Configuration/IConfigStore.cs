namespace ZakYip.NarrowBeltDiverterSorter.Infrastructure.Configuration;

/// <summary>
/// 通用配置存储接口
/// </summary>
[Obsolete("请使用 Core.Configuration.ISorterConfigurationStore 和 Infrastructure.LiteDb.LiteDbSorterConfigurationStore 代替。此接口将在未来版本中移除。")]
public interface IConfigStore
{
    /// <summary>
    /// 异步加载配置
    /// </summary>
    /// <typeparam name="T">配置对象类型</typeparam>
    /// <param name="key">配置键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>配置对象，如果不存在则返回 null</returns>
    Task<T?> LoadAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// 异步保存配置
    /// </summary>
    /// <typeparam name="T">配置对象类型</typeparam>
    /// <param name="key">配置键</param>
    /// <param name="options">配置对象</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task SaveAsync<T>(string key, T options, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// 检查配置是否存在
    /// </summary>
    /// <param name="key">配置键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>如果配置存在则返回 true，否则返回 false</returns>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
}

namespace ZakYip.NarrowBeltDiverterSorter.Core.Configuration;

/// <summary>
/// 应用配置存储接口
/// 提供统一的配置读写能力，由 Infrastructure 层实现（如 LiteDB 持久化）
/// 读取失败时应返回 null 而不抛出异常，确保应用能够使用默认值启动
/// </summary>
public interface IAppConfigurationStore
{
    /// <summary>
    /// 异步加载配置对象
    /// </summary>
    /// <typeparam name="T">配置类型</typeparam>
    /// <param name="key">配置键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>配置对象，如果不存在或读取失败则返回 null</returns>
    Task<T?> LoadAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// 异步保存配置对象
    /// </summary>
    /// <typeparam name="T">配置类型</typeparam>
    /// <param name="key">配置键</param>
    /// <param name="value">配置对象</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task SaveAsync<T>(string key, T value, CancellationToken cancellationToken = default) where T : class;
}

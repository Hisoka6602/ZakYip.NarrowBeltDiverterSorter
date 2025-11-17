namespace ZakYip.NarrowBeltDiverterSorter.Core.Configuration;

/// <summary>
/// 分拣机配置存储接口
/// 此接口仅用于系统运行所需的配置对象（如拓扑、分拣模式、设备连接参数等），
/// 不用于日志、高频事件或统计数据。
/// </summary>
public interface ISorterConfigurationStore
{
    /// <summary>
    /// 异步加载配置对象
    /// </summary>
    /// <typeparam name="T">配置对象类型</typeparam>
    /// <param name="key">配置键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>配置对象，如果不存在则返回 null</returns>
    Task<T?> LoadAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// 异步保存配置对象
    /// </summary>
    /// <typeparam name="T">配置对象类型</typeparam>
    /// <param name="key">配置键</param>
    /// <param name="value">配置对象</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task SaveAsync<T>(string key, T value, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// 检查配置是否存在
    /// </summary>
    /// <param name="key">配置键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>如果配置存在则返回 true，否则返回 false</returns>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
}

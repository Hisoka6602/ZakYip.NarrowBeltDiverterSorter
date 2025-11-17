using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Core.Configuration;

namespace ZakYip.NarrowBeltDiverterSorter.Infrastructure.Configuration;

/// <summary>
/// 基于 LiteDB 的应用配置存储实现
/// 包装 ISorterConfigurationStore，提供防御式错误处理，确保配置读取失败时不影响应用启动
/// </summary>
public sealed class LiteDbAppConfigurationStore : IAppConfigurationStore
{
    private readonly ISorterConfigurationStore _innerStore;
    private readonly ILogger<LiteDbAppConfigurationStore> _logger;

    public LiteDbAppConfigurationStore(
        ISorterConfigurationStore innerStore,
        ILogger<LiteDbAppConfigurationStore> logger)
    {
        _innerStore = innerStore ?? throw new ArgumentNullException(nameof(innerStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<T?> LoadAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var result = await _innerStore.LoadAsync<T>(key, cancellationToken);
            
            if (result == null)
            {
                _logger.LogDebug("配置键 '{Key}' 不存在，将使用默认值", key);
            }
            else
            {
                _logger.LogDebug("成功加载配置键 '{Key}'", key);
            }
            
            return result;
        }
        catch (ConfigurationAccessException ex)
        {
            _logger.LogWarning(ex, "加载配置键 '{Key}' 失败: {Message}，将使用默认值", key, ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载配置键 '{Key}' 时发生意外错误: {Message}，将使用默认值", key, ex.Message);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task SaveAsync<T>(string key, T value, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            await _innerStore.SaveAsync(key, value, cancellationToken);
            _logger.LogInformation("成功保存配置键 '{Key}'", key);
        }
        catch (ConfigurationAccessException ex)
        {
            _logger.LogError(ex, "保存配置键 '{Key}' 失败: {Message}", key, ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存配置键 '{Key}' 时发生意外错误: {Message}", key, ex.Message);
            throw;
        }
    }
}

using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Core.Configuration;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Feeding;

namespace ZakYip.NarrowBeltDiverterSorter.Infrastructure.Configuration;

/// <summary>
/// 基于 LiteDB 的供包容量配置仓储
/// </summary>
public class LiteDbFeedingCapacityOptionsRepository : IFeedingCapacityOptionsRepository
{
    private const string ConfigKey = "FeedingCapacityOptions";
    private readonly ISorterConfigurationStore _configStore;
    private readonly ILogger<LiteDbFeedingCapacityOptionsRepository> _logger;

    /// <summary>
    /// 初始化供包容量配置仓储
    /// </summary>
    public LiteDbFeedingCapacityOptionsRepository(
        ISorterConfigurationStore configStore,
        ILogger<LiteDbFeedingCapacityOptionsRepository> logger)
    {
        _configStore = configStore ?? throw new ArgumentNullException(nameof(configStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<FeedingCapacityOptions> LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var options = await _configStore.LoadAsync<FeedingCapacityOptions>(ConfigKey, cancellationToken);
            
            if (options == null)
            {
                _logger.LogInformation("供包容量配置不存在，创建默认配置");
                options = new FeedingCapacityOptions();
                await SaveAsync(options, cancellationToken);
            }
            
            return options;
        }
        catch (ConfigurationAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            var message = $"加载供包容量配置失败: {ex.Message}";
            _logger.LogError(ex, message);
            throw new ConfigurationAccessException(message, ex);
        }
    }

    /// <inheritdoc/>
    public async Task SaveAsync(FeedingCapacityOptions options, CancellationToken cancellationToken = default)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        try
        {
            await _configStore.SaveAsync(ConfigKey, options, cancellationToken);
            _logger.LogInformation("已保存供包容量配置");
        }
        catch (ConfigurationAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            var message = $"保存供包容量配置失败: {ex.Message}";
            _logger.LogError(ex, message);
            throw new ConfigurationAccessException(message, ex);
        }
    }
}

using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Core.Configuration;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Feeding;

namespace ZakYip.NarrowBeltDiverterSorter.Infrastructure.Configuration;

/// <summary>
/// 基于 LiteDB 的入口布局选项仓储
/// </summary>
public class LiteDbInfeedLayoutOptionsRepository : IInfeedLayoutOptionsRepository
{
    private const string ConfigKey = "InfeedLayoutOptions";
    private readonly ISorterConfigurationStore _configStore;
    private readonly ILogger<LiteDbInfeedLayoutOptionsRepository> _logger;

    /// <summary>
    /// 初始化入口布局选项仓储
    /// </summary>
    public LiteDbInfeedLayoutOptionsRepository(
        ISorterConfigurationStore configStore,
        ILogger<LiteDbInfeedLayoutOptionsRepository> logger)
    {
        _configStore = configStore ?? throw new ArgumentNullException(nameof(configStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<InfeedLayoutOptions> LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var options = await _configStore.LoadAsync<InfeedLayoutOptions>(ConfigKey, cancellationToken);
            
            if (options == null)
            {
                _logger.LogInformation("入口布局选项不存在，创建默认配置");
                options = InfeedLayoutOptions.CreateDefault();
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
            var message = $"加载入口布局选项失败: {ex.Message}";
            _logger.LogError(ex, message);
            throw new ConfigurationAccessException(message, ex);
        }
    }

    /// <inheritdoc/>
    public async Task SaveAsync(InfeedLayoutOptions options, CancellationToken cancellationToken = default)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        try
        {
            await _configStore.SaveAsync(ConfigKey, options, cancellationToken);
            _logger.LogInformation("已保存入口布局选项");
        }
        catch (ConfigurationAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            var message = $"保存入口布局选项失败: {ex.Message}";
            _logger.LogError(ex, message);
            throw new ConfigurationAccessException(message, ex);
        }
    }
}

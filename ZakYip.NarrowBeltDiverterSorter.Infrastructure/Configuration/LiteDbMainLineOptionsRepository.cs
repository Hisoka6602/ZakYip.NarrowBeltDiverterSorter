using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.MainLine;

namespace ZakYip.NarrowBeltDiverterSorter.Infrastructure.Configuration;

/// <summary>
/// 基于 LiteDB 的主线控制选项仓储
/// </summary>
public class LiteDbMainLineOptionsRepository : IMainLineOptionsRepository
{
    private const string ConfigKey = "MainLineControlOptions";
    private readonly IConfigStore _configStore;
    private readonly ILogger<LiteDbMainLineOptionsRepository> _logger;

    /// <summary>
    /// 初始化主线控制选项仓储
    /// </summary>
    public LiteDbMainLineOptionsRepository(
        IConfigStore configStore,
        ILogger<LiteDbMainLineOptionsRepository> logger)
    {
        _configStore = configStore ?? throw new ArgumentNullException(nameof(configStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<MainLineControlOptions> LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var options = await _configStore.LoadAsync<MainLineControlOptions>(ConfigKey, cancellationToken);
            
            if (options == null)
            {
                _logger.LogInformation("主线控制选项不存在，创建默认配置");
                options = MainLineControlOptions.CreateDefault();
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
            var message = $"加载主线控制选项失败: {ex.Message}";
            _logger.LogError(ex, message);
            throw new ConfigurationAccessException(message, ex);
        }
    }

    /// <inheritdoc/>
    public async Task SaveAsync(MainLineControlOptions options, CancellationToken cancellationToken = default)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        try
        {
            await _configStore.SaveAsync(ConfigKey, options, cancellationToken);
            _logger.LogInformation("已保存主线控制选项");
        }
        catch (ConfigurationAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            var message = $"保存主线控制选项失败: {ex.Message}";
            _logger.LogError(ex, message);
            throw new ConfigurationAccessException(message, ex);
        }
    }
}

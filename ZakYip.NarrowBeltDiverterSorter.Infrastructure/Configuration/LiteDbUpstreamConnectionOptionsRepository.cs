using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Communication.Upstream;

namespace ZakYip.NarrowBeltDiverterSorter.Infrastructure.Configuration;

/// <summary>
/// 基于 LiteDB 的上游连接选项仓储
/// </summary>
public class LiteDbUpstreamConnectionOptionsRepository : IUpstreamConnectionOptionsRepository
{
    private const string ConfigKey = "UpstreamConnectionOptions";
    private readonly IConfigStore _configStore;
    private readonly ILogger<LiteDbUpstreamConnectionOptionsRepository> _logger;

    /// <summary>
    /// 初始化上游连接选项仓储
    /// </summary>
    public LiteDbUpstreamConnectionOptionsRepository(
        IConfigStore configStore,
        ILogger<LiteDbUpstreamConnectionOptionsRepository> logger)
    {
        _configStore = configStore ?? throw new ArgumentNullException(nameof(configStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<UpstreamConnectionOptions> LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var options = await _configStore.LoadAsync<UpstreamConnectionOptions>(ConfigKey, cancellationToken);
            
            if (options == null)
            {
                _logger.LogInformation("上游连接选项不存在，创建默认配置");
                options = UpstreamConnectionOptions.CreateDefault();
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
            var message = $"加载上游连接选项失败: {ex.Message}";
            _logger.LogError(ex, message);
            throw new ConfigurationAccessException(message, ex);
        }
    }

    /// <inheritdoc/>
    public async Task SaveAsync(UpstreamConnectionOptions options, CancellationToken cancellationToken = default)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        try
        {
            await _configStore.SaveAsync(ConfigKey, options, cancellationToken);
            _logger.LogInformation("已保存上游连接选项");
        }
        catch (ConfigurationAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            var message = $"保存上游连接选项失败: {ex.Message}";
            _logger.LogError(ex, message);
            throw new ConfigurationAccessException(message, ex);
        }
    }
}

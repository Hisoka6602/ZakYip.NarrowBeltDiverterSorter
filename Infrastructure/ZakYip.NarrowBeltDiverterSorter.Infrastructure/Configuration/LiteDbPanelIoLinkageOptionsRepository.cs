using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Core.Configuration;

namespace ZakYip.NarrowBeltDiverterSorter.Infrastructure.Configuration;

/// <summary>
/// 基于 LiteDB 的面板 IO 联动选项仓储
/// </summary>
public class LiteDbPanelIoLinkageOptionsRepository : IPanelIoLinkageOptionsRepository
{
    private const string ConfigKey = "PanelIoLinkageOptions";
    private readonly ISorterConfigurationStore _configStore;
    private readonly ILogger<LiteDbPanelIoLinkageOptionsRepository> _logger;

    /// <summary>
    /// 初始化面板 IO 联动选项仓储
    /// </summary>
    public LiteDbPanelIoLinkageOptionsRepository(
        ISorterConfigurationStore configStore,
        ILogger<LiteDbPanelIoLinkageOptionsRepository> logger)
    {
        _configStore = configStore ?? throw new ArgumentNullException(nameof(configStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<PanelIoLinkageOptions> LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var options = await _configStore.LoadAsync<PanelIoLinkageOptions>(ConfigKey, cancellationToken);
            
            if (options == null)
            {
                _logger.LogInformation("面板 IO 联动选项不存在，创建默认配置");
                options = new PanelIoLinkageOptions
                {
                    StartFollowOutputChannels = Array.Empty<int>(),
                    StopFollowOutputChannels = Array.Empty<int>(),
                    FirstStableSpeedFollowOutputChannels = Array.Empty<int>(),
                    UnstableAfterStableFollowOutputChannels = Array.Empty<int>()
                };
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
            var message = $"加载面板 IO 联动选项失败: {ex.Message}";
            _logger.LogError(ex, message);
            throw new ConfigurationAccessException(message, ex);
        }
    }

    /// <inheritdoc/>
    public async Task SaveAsync(PanelIoLinkageOptions options, CancellationToken cancellationToken = default)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        try
        {
            await _configStore.SaveAsync(ConfigKey, options, cancellationToken);
            _logger.LogInformation("已保存面板 IO 联动选项");
        }
        catch (ConfigurationAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            var message = $"保存面板 IO 联动选项失败: {ex.Message}";
            _logger.LogError(ex, message);
            throw new ConfigurationAccessException(message, ex);
        }
    }
}

using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Sorting;

namespace ZakYip.NarrowBeltDiverterSorter.Infrastructure.Configuration;

/// <summary>
/// 基于 LiteDB 的格口配置仓储
/// </summary>
public class LiteDbChuteConfigRepository : IChuteConfigRepository
{
    private const string ConfigKey = "ChuteConfigs";
    private readonly IConfigStore _configStore;
    private readonly ILogger<LiteDbChuteConfigRepository> _logger;

    /// <summary>
    /// 初始化格口配置仓储
    /// </summary>
    public LiteDbChuteConfigRepository(
        IConfigStore configStore,
        ILogger<LiteDbChuteConfigRepository> logger)
    {
        _configStore = configStore ?? throw new ArgumentNullException(nameof(configStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<ChuteConfigSet> LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var configSet = await _configStore.LoadAsync<ChuteConfigSet>(ConfigKey, cancellationToken);
            
            if (configSet == null)
            {
                _logger.LogInformation("格口配置不存在，创建默认配置");
                configSet = ChuteConfigSet.CreateDefault();
                await SaveAsync(configSet, cancellationToken);
            }
            
            return configSet;
        }
        catch (ConfigurationAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            var message = $"加载格口配置失败: {ex.Message}";
            _logger.LogError(ex, message);
            throw new ConfigurationAccessException(message, ex);
        }
    }

    /// <inheritdoc/>
    public async Task SaveAsync(ChuteConfigSet configSet, CancellationToken cancellationToken = default)
    {
        if (configSet == null)
        {
            throw new ArgumentNullException(nameof(configSet));
        }

        try
        {
            await _configStore.SaveAsync(ConfigKey, configSet, cancellationToken);
            _logger.LogInformation("已保存格口配置");
        }
        catch (ConfigurationAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            var message = $"保存格口配置失败: {ex.Message}";
            _logger.LogError(ex, message);
            throw new ConfigurationAccessException(message, ex);
        }
    }
}

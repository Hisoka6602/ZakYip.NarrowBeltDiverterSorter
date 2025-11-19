using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Core.Configuration;

#pragma warning disable CS0618 // IConfigStore 已过时：此类使用旧的 IConfigStore 接口以保持向后兼容性。新代码应使用 ISorterConfigurationStore 和 LiteDbSorterConfigurationStore。

namespace ZakYip.NarrowBeltDiverterSorter.Infrastructure.Configuration;

/// <summary>
/// 基于 LiteDB 的长跑高负载测试选项仓储。
/// </summary>
public sealed class LiteDbLongRunLoadTestOptionsRepository : ILongRunLoadTestOptionsRepository
{
    private const string ConfigKey = "LongRunLoadTestOptions";
    private readonly IConfigStore _configStore;
    private readonly ILogger<LiteDbLongRunLoadTestOptionsRepository> _logger;

    public LiteDbLongRunLoadTestOptionsRepository(
        IConfigStore configStore,
        ILogger<LiteDbLongRunLoadTestOptionsRepository> logger)
    {
        _configStore = configStore ?? throw new ArgumentNullException(nameof(configStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<LongRunLoadTestOptions> LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var options = await _configStore.LoadAsync<LongRunLoadTestOptions>(ConfigKey, cancellationToken);
            if (options != null)
            {
                _logger.LogInformation("已从数据库加载长跑测试配置");
                return options;
            }

            _logger.LogInformation("数据库中无长跑测试配置，使用默认配置");
            var defaultOptions = LongRunLoadTestOptions.CreateDefault();
            await SaveAsync(defaultOptions, cancellationToken);
            return defaultOptions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载长跑测试配置失败，使用默认配置");
            return LongRunLoadTestOptions.CreateDefault();
        }
    }

    public async Task SaveAsync(LongRunLoadTestOptions options, CancellationToken cancellationToken = default)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        try
        {
            await _configStore.SaveAsync(ConfigKey, options, cancellationToken);
            _logger.LogInformation("长跑测试配置已保存到数据库");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存长跑测试配置失败");
            throw new ConfigurationAccessException("保存长跑测试配置失败", ex);
        }
    }
}

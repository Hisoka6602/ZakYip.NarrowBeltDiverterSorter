using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Configuration;

namespace ZakYip.NarrowBeltDiverterSorter.Infrastructure.Configuration;

/// <summary>
/// 基于 LiteDB 的上游路由配置提供器
/// 提供运行时配置访问和变更通知
/// </summary>
public class LiteDbUpstreamRoutingConfigProvider : IUpstreamRoutingConfigProvider
{
    private const string ConfigKey = "UpstreamRoutingOptions";
    private readonly ISorterConfigurationStore _configStore;
    private readonly ILogger<LiteDbUpstreamRoutingConfigProvider> _logger;
    private UpstreamRoutingOptions _currentOptions;
    private readonly object _lock = new();

    /// <inheritdoc/>
    public event EventHandler<UpstreamRoutingConfigChangedEventArgs>? ConfigChanged;

    /// <summary>
    /// 初始化上游路由配置提供器
    /// </summary>
    public LiteDbUpstreamRoutingConfigProvider(
        ISorterConfigurationStore configStore,
        ILogger<LiteDbUpstreamRoutingConfigProvider> logger)
    {
        _configStore = configStore ?? throw new ArgumentNullException(nameof(configStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // 初始化时加载配置
        _currentOptions = LoadFromStoreAsync().GetAwaiter().GetResult();
    }

    /// <inheritdoc/>
    public UpstreamRoutingOptions GetCurrentOptions()
    {
        lock (_lock)
        {
            // 返回副本以防止外部修改
            return new UpstreamRoutingOptions
            {
                UpstreamResultTtl = _currentOptions.UpstreamResultTtl,
                ErrorChuteId = _currentOptions.ErrorChuteId
            };
        }
    }

    /// <summary>
    /// 更新配置（由管理接口调用）
    /// </summary>
    /// <param name="newOptions">新的配置选项</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task UpdateOptionsAsync(UpstreamRoutingOptions newOptions, CancellationToken cancellationToken = default)
    {
        if (newOptions == null)
        {
            throw new ArgumentNullException(nameof(newOptions));
        }

        try
        {
            // 保存到 LiteDB
            await _configStore.SaveAsync(ConfigKey, newOptions, cancellationToken);

            // 更新内存快照
            lock (_lock)
            {
                _currentOptions = newOptions;
            }

            _logger.LogInformation(
                "上游路由配置已更新：TTL={TtlSeconds}秒，异常格口={ErrorChuteId}",
                newOptions.UpstreamResultTtl.TotalSeconds,
                newOptions.ErrorChuteId);

            // 触发配置变更事件
            ConfigChanged?.Invoke(this, new UpstreamRoutingConfigChangedEventArgs
            {
                NewOptions = newOptions
            });
        }
        catch (Exception ex)
        {
            var message = $"更新上游路由配置失败: {ex.Message}";
            _logger.LogError(ex, message);
            throw new ConfigurationAccessException(message, ex);
        }
    }

    private async Task<UpstreamRoutingOptions> LoadFromStoreAsync()
    {
        try
        {
            var options = await _configStore.LoadAsync<UpstreamRoutingOptions>(ConfigKey);

            if (options == null)
            {
                _logger.LogInformation("上游路由配置不存在，创建默认配置");
                options = UpstreamRoutingOptions.CreateDefault();
                await _configStore.SaveAsync(ConfigKey, options);
            }

            _logger.LogInformation(
                "已加载上游路由配置：TTL={TtlSeconds}秒，异常格口={ErrorChuteId}",
                options.UpstreamResultTtl.TotalSeconds,
                options.ErrorChuteId);

            return options;
        }
        catch (ConfigurationAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            var message = $"加载上游路由配置失败: {ex.Message}";
            _logger.LogError(ex, message);
            throw new ConfigurationAccessException(message, ex);
        }
    }
}

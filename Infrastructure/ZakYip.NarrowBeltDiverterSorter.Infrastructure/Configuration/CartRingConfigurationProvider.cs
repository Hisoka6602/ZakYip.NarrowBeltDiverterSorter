using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Core.Configuration;

namespace ZakYip.NarrowBeltDiverterSorter.Infrastructure.Configuration;

/// <summary>
/// 小车环配置提供器实现
/// 基于 LiteDB 存储，支持运行期热更新
/// </summary>
public sealed class CartRingConfigurationProvider : ICartRingConfigurationProvider
{
    private const string ConfigKey = "CartRing";
    private readonly ISorterConfigurationStore _store;
    private readonly ILogger<CartRingConfigurationProvider> _logger;
    private CartRingConfiguration? _cachedConfig;
    private readonly object _cacheLock = new();

    public CartRingConfigurationProvider(
        ISorterConfigurationStore store,
        ILogger<CartRingConfigurationProvider> logger)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public CartRingConfiguration Current
    {
        get
        {
            lock (_cacheLock)
            {
                if (_cachedConfig == null)
                {
                    // 同步加载（在启动时应该已经调用过 LoadAsync）
                    _cachedConfig = LoadAsync(CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();
                }
                return _cachedConfig;
            }
        }
    }

    /// <inheritdoc/>
    public async Task<CartRingConfiguration> LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // 尝试从 LiteDB 加载配置
            var config = await _store.LoadAsync<CartRingConfiguration>(ConfigKey, cancellationToken);

            if (config != null)
            {
                _logger.LogInformation("从 LiteDB 加载小车环配置成功，TotalCartCount: {TotalCartCount}", config.TotalCartCount);
                lock (_cacheLock)
                {
                    _cachedConfig = config;
                }
                return config;
            }

            // LiteDB 中没有配置，使用默认值
            _logger.LogWarning("LiteDB 中未找到小车环配置，将使用默认值（自动学习模式）");
            config = CartRingConfiguration.CreateDefault();

            // 保存到 LiteDB
            await _store.SaveAsync(ConfigKey, config, cancellationToken);
            _logger.LogInformation("已将默认小车环配置写入 LiteDB，TotalCartCount: {TotalCartCount}", config.TotalCartCount);

            lock (_cacheLock)
            {
                _cachedConfig = config;
            }
            return config;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载小车环配置失败");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(CartRingConfiguration configuration, CancellationToken cancellationToken = default)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        try
        {
            await _store.SaveAsync(ConfigKey, configuration, cancellationToken);
            _logger.LogInformation("已更新小车环配置，TotalCartCount: {TotalCartCount}", configuration.TotalCartCount);

            lock (_cacheLock)
            {
                _cachedConfig = configuration;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新小车环配置失败");
            throw;
        }
    }
}

using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Core.Configuration;

namespace ZakYip.NarrowBeltDiverterSorter.Infrastructure.Configuration;

/// <summary>
/// Sorter 配置提供器实现
/// 基于 LiteDB 存储，使用内置默认值作为回退
/// </summary>
public sealed class SorterConfigurationProvider : ISorterConfigurationProvider
{
    private const string ConfigKey = "Sorter";
    private readonly ISorterConfigurationStore _store;
    private readonly ILogger<SorterConfigurationProvider> _logger;
    private SorterOptions? _cachedOptions;
    private readonly object _cacheLock = new();

    /// <summary>
    /// 内置默认配置
    /// 此默认值仅用于 LiteDB 中完全缺失配置时的回退
    /// 与之前 appsettings 中的默认值保持一致
    /// </summary>
    private static readonly SorterOptions DefaultSorterOptions = new()
    {
        MainLine = new SorterMainLineOptions
        {
            Mode = "Simulation",
            Rema = new RemaConnectionOptions
            {
                PortName = "COM3",
                BaudRate = 38400,
                DataBits = 8,
                Parity = "None",
                StopBits = "One",
                SlaveAddress = 1,
                ReadTimeout = TimeSpan.FromMilliseconds(1200),
                WriteTimeout = TimeSpan.FromMilliseconds(1200),
                ConnectTimeout = TimeSpan.FromSeconds(3),
                MaxRetries = 3,
                RetryDelay = TimeSpan.FromMilliseconds(200)
            }
        }
    };

    public SorterConfigurationProvider(
        ISorterConfigurationStore store,
        ILogger<SorterConfigurationProvider> logger)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public SorterOptions Current
    {
        get
        {
            lock (_cacheLock)
            {
                if (_cachedOptions == null)
                {
                    // 同步加载（在启动时应该已经调用过 LoadAsync）
                    _cachedOptions = LoadAsync().GetAwaiter().GetResult();
                }
                return _cachedOptions;
            }
        }
    }

    /// <inheritdoc/>
    public async Task<SorterOptions> LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // 尝试从 LiteDB 加载配置
            var options = await _store.LoadAsync<SorterOptions>(ConfigKey, cancellationToken);

            if (options != null)
            {
                _logger.LogInformation("从 LiteDB 加载 Sorter 配置成功，模式: {Mode}", options.MainLine.Mode);
                lock (_cacheLock)
                {
                    _cachedOptions = options;
                }
                return options;
            }

            // LiteDB 中没有配置，使用内置默认值
            _logger.LogWarning("LiteDB 中未找到 Sorter 配置，将使用内置默认值");
            options = CloneDefaultOptions();

            // 保存到 LiteDB
            await _store.SaveAsync(ConfigKey, options, cancellationToken);
            _logger.LogInformation("已将内置默认 Sorter 配置写入 LiteDB，模式: {Mode}", options.MainLine.Mode);

            lock (_cacheLock)
            {
                _cachedOptions = options;
            }
            return options;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载 Sorter 配置失败");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(SorterOptions options, CancellationToken cancellationToken = default)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        try
        {
            await _store.SaveAsync(ConfigKey, options, cancellationToken);
            _logger.LogInformation("已更新 Sorter 配置，模式: {Mode}", options.MainLine.Mode);

            lock (_cacheLock)
            {
                _cachedOptions = options;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新 Sorter 配置失败");
            throw;
        }
    }

    /// <summary>
    /// 克隆内置默认配置
    /// </summary>
    private static SorterOptions CloneDefaultOptions()
    {
        return new SorterOptions
        {
            MainLine = new SorterMainLineOptions
            {
                Mode = DefaultSorterOptions.MainLine.Mode,
                Rema = new RemaConnectionOptions
                {
                    PortName = DefaultSorterOptions.MainLine.Rema.PortName,
                    BaudRate = DefaultSorterOptions.MainLine.Rema.BaudRate,
                    DataBits = DefaultSorterOptions.MainLine.Rema.DataBits,
                    Parity = DefaultSorterOptions.MainLine.Rema.Parity,
                    StopBits = DefaultSorterOptions.MainLine.Rema.StopBits,
                    SlaveAddress = DefaultSorterOptions.MainLine.Rema.SlaveAddress,
                    ReadTimeout = DefaultSorterOptions.MainLine.Rema.ReadTimeout,
                    WriteTimeout = DefaultSorterOptions.MainLine.Rema.WriteTimeout,
                    ConnectTimeout = DefaultSorterOptions.MainLine.Rema.ConnectTimeout,
                    MaxRetries = DefaultSorterOptions.MainLine.Rema.MaxRetries,
                    RetryDelay = DefaultSorterOptions.MainLine.Rema.RetryDelay
                }
            }
        };
    }
}

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Core.Configuration;

namespace ZakYip.NarrowBeltDiverterSorter.Infrastructure.Configuration;

/// <summary>
/// Sorter 配置提供器实现
/// 基于 LiteDB 存储，支持从 appsettings.json 初始化默认配置
/// </summary>
public sealed class SorterConfigurationProvider : ISorterConfigurationProvider
{
    private const string ConfigKey = "Sorter";
    private readonly ISorterConfigurationStore _store;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SorterConfigurationProvider> _logger;
    private SorterOptions? _cachedOptions;
    private readonly object _cacheLock = new();

    public SorterConfigurationProvider(
        ISorterConfigurationStore store,
        IConfiguration configuration,
        ILogger<SorterConfigurationProvider> logger)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
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

            // LiteDB 中没有配置，从 appsettings.json 读取默认值
            _logger.LogInformation("LiteDB 中未找到 Sorter 配置，将从 appsettings.json 初始化默认配置");
            options = LoadDefaultFromConfiguration();

            // 保存到 LiteDB
            await _store.SaveAsync(ConfigKey, options, cancellationToken);
            _logger.LogInformation("已将默认 Sorter 配置保存到 LiteDB，模式: {Mode}", options.MainLine.Mode);

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
    /// 从 IConfiguration 加载默认配置
    /// </summary>
    private SorterOptions LoadDefaultFromConfiguration()
    {
        var options = new SorterOptions
        {
            MainLine = new SorterMainLineOptions()
        };

        // 绑定配置节
        var section = _configuration.GetSection("Sorter:MainLine");
        if (section.Exists())
        {
            options.MainLine.Mode = section.GetValue<string>("Mode") ?? "Simulation";
            
            // 读取 Rema 配置
            var remaSection = section.GetSection("Rema");
            if (remaSection.Exists())
            {
                options.MainLine.Rema = new RemaConnectionOptions
                {
                    PortName = remaSection.GetValue<string>("PortName") ?? "COM3",
                    BaudRate = remaSection.GetValue<int>("BaudRate", 38400),
                    DataBits = remaSection.GetValue<int>("DataBits", 8),
                    Parity = remaSection.GetValue<string>("Parity") ?? "None",
                    StopBits = remaSection.GetValue<string>("StopBits") ?? "One",
                    SlaveAddress = remaSection.GetValue<int>("SlaveAddress", 1),
                    ReadTimeout = remaSection.GetValue<TimeSpan>("ReadTimeout", TimeSpan.FromMilliseconds(1200)),
                    WriteTimeout = remaSection.GetValue<TimeSpan>("WriteTimeout", TimeSpan.FromMilliseconds(1200)),
                    ConnectTimeout = remaSection.GetValue<TimeSpan>("ConnectTimeout", TimeSpan.FromSeconds(3)),
                    MaxRetries = remaSection.GetValue<int>("MaxRetries", 3),
                    RetryDelay = remaSection.GetValue<TimeSpan>("RetryDelay", TimeSpan.FromMilliseconds(200))
                };
            }
        }

        _logger.LogInformation("从 appsettings.json 读取默认 Sorter 配置，模式: {Mode}", options.MainLine.Mode);
        return options;
    }
}

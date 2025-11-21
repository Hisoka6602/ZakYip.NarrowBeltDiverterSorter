using ZakYip.NarrowBeltDiverterSorter.Communication.Upstream;
using ZakYip.NarrowBeltDiverterSorter.Core.Configuration;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Feeding;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.MainLine;

namespace ZakYip.NarrowBeltDiverterSorter.Host.Configuration;

/// <summary>
/// Host 配置提供器实现
/// 通过"默认值 + LiteDB 覆盖"的模式提供所有配置
/// 确保无论 LiteDB 是否可用，Host 都能正常启动
/// </summary>
public sealed class HostConfigurationProvider : IHostConfigurationProvider
{
    private readonly IAppConfigurationStore _store;
    private readonly IConfigurationDefaultsProvider _defaultsProvider;
    private readonly ILogger<HostConfigurationProvider> _logger;

    // 配置键常量
    private const string MainLineControlKey = "MainLineControl";
    private const string InfeedLayoutKey = "InfeedLayout";
    private const string UpstreamConnectionKey = "UpstreamConnection";
    private const string UpstreamKey = "Upstream";
    private const string SimulationKey = "Simulation";
    private const string SafetyKey = "Safety";
    private const string RecordingKey = "Recording";
    private const string SignalRPushKey = "SignalRPush";
    private const string RemaLm1000HKey = "RemaLm1000H";
    private const string ChuteIoKey = "ChuteIo";
    private const string LongRunLoadTestKey = "LongRunLoadTest";

    public HostConfigurationProvider(
        IAppConfigurationStore store,
        IConfigurationDefaultsProvider defaultsProvider,
        ILogger<HostConfigurationProvider> logger)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _defaultsProvider = defaultsProvider ?? throw new ArgumentNullException(nameof(defaultsProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<MainLineControlOptions> GetMainLineControlOptionsAsync(CancellationToken ct = default)
    {
        return await GetConfigurationAsync<MainLineControlOptions>(MainLineControlKey, ct);
    }

    public async Task<InfeedLayoutOptions> GetInfeedLayoutOptionsAsync(CancellationToken ct = default)
    {
        return await GetConfigurationAsync<InfeedLayoutOptions>(InfeedLayoutKey, ct);
    }

    public async Task<UpstreamOptions> GetUpstreamOptionsAsync(CancellationToken ct = default)
    {
        // 创建默认配置
        var defaultConfig = new UpstreamOptions
        {
            Mode = UpstreamMode.Disabled,
            Mqtt = new MqttOptions(),
            Tcp = new TcpOptions()
        };
        
        var storedConfig = await _store.LoadAsync<UpstreamOptions>(UpstreamKey, ct);
        
        if (storedConfig != null)
        {
            _logger.LogDebug("使用 LiteDB 中的配置: {Key}", UpstreamKey);
            return storedConfig;
        }
        else
        {
            _logger.LogInformation("未找到 LiteDB 配置 '{Key}'，使用默认值", UpstreamKey);
            return defaultConfig;
        }
    }

    public async Task<NarrowBeltSimulationOptions> GetSimulationOptionsAsync(CancellationToken ct = default)
    {
        return await GetConfigurationAsync<NarrowBeltSimulationOptions>(SimulationKey, ct);
    }

    public async Task<SafetyConfiguration> GetSafetyConfigurationAsync(CancellationToken ct = default)
    {
        return await GetConfigurationAsync<SafetyConfiguration>(SafetyKey, ct);
    }

    public async Task<RecordingConfiguration> GetRecordingConfigurationAsync(CancellationToken ct = default)
    {
        return await GetConfigurationAsync<RecordingConfiguration>(RecordingKey, ct);
    }

    public async Task<SignalRPushConfiguration> GetSignalRPushConfigurationAsync(CancellationToken ct = default)
    {
        return await GetConfigurationAsync<SignalRPushConfiguration>(SignalRPushKey, ct);
    }

    public async Task<RemaLm1000HConfiguration> GetRemaLm1000HConfigurationAsync(CancellationToken ct = default)
    {
        return await GetConfigurationAsync<RemaLm1000HConfiguration>(RemaLm1000HKey, ct);
    }

    public async Task<ChuteIoConfiguration> GetChuteIoConfigurationAsync(CancellationToken ct = default)
    {
        return await GetConfigurationAsync<ChuteIoConfiguration>(ChuteIoKey, ct);
    }

    public async Task<LongRunLoadTestOptions> GetLongRunLoadTestOptionsAsync(CancellationToken ct = default)
    {
        return await GetConfigurationAsync<LongRunLoadTestOptions>(LongRunLoadTestKey, ct);
    }

    /// <summary>
    /// 通用配置获取方法：默认值 + LiteDB 覆盖
    /// </summary>
    private async Task<T> GetConfigurationAsync<T>(string key, CancellationToken ct) where T : class
    {
        // 1. 获取默认值
        var defaultConfig = _defaultsProvider.GetDefaults<T>();
        
        // 2. 尝试从 LiteDB 加载
        var storedConfig = await _store.LoadAsync<T>(key, ct);
        
        // 3. 如果 LiteDB 中有配置，则使用 LiteDB 配置；否则使用默认值
        if (storedConfig != null)
        {
            _logger.LogDebug("使用 LiteDB 中的配置: {Key}", key);
            return storedConfig;
        }
        else
        {
            _logger.LogInformation("未找到 LiteDB 配置 '{Key}'，使用默认值", key);
            return defaultConfig;
        }
    }
}

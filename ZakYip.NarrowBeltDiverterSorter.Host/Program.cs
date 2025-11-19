using Microsoft.Extensions.Options;
using ZakYip.NarrowBeltDiverterSorter.Communication;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Application;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Parcels;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.MainLine;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Tracking;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Feeding;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Sorting;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Carts;
using ZakYip.NarrowBeltDiverterSorter.Execution.Cart;
using ZakYip.NarrowBeltDiverterSorter.Execution.Chute;
using ZakYip.NarrowBeltDiverterSorter.Execution.Chute.Drivers.ZhiQian32Relay;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Execution.Mainline;
using ZakYip.NarrowBeltDiverterSorter.Execution.Vendors.Simulated;
using ZakYip.NarrowBeltDiverterSorter.Execution.Vendors.Rema;
using ZakYip.NarrowBeltDiverterSorter.Execution.Feeding;
using ZakYip.NarrowBeltDiverterSorter.Execution.Sorting;
using ChuteSafetyService = ZakYip.NarrowBeltDiverterSorter.Execution.Sorting.ChuteSafetyService;
using ZakYip.NarrowBeltDiverterSorter.Execution.Safety;
using ZakYip.NarrowBeltDiverterSorter.Observability;
using ZakYip.NarrowBeltDiverterSorter.Observability.LiveView;
using ZakYip.NarrowBeltDiverterSorter.Ingress.Chute;
using ZakYip.NarrowBeltDiverterSorter.Core.Configuration;
using ZakYip.NarrowBeltDiverterSorter.Infrastructure;
using ZakYip.NarrowBeltDiverterSorter.Infrastructure.Configuration;
using ZakYip.NarrowBeltDiverterSorter.Infrastructure.LiteDb;
using ZakYip.NarrowBeltDiverterSorter.Communication.Upstream;
using ZakYip.NarrowBeltDiverterSorter.Host;
using ZakYip.NarrowBeltDiverterSorter.Host.SignalR;
using ZakYip.NarrowBeltDiverterSorter.Observability.Recording;
// Note: Simulation types cannot be used due to circular dependency
// using ZakYip.NarrowBeltDiverterSorter.Simulation;
// using ZakYip.NarrowBeltDiverterSorter.Simulation.Fakes;

// ============================================================================
// 解析启动模式参数
// ============================================================================

var startupConfig = StartupModeConfiguration.ParseFromArgs(args);
Console.WriteLine($"启动模式: {startupConfig.GetModeDescription()}");

var builder = WebApplication.CreateBuilder(args);

// 注册启动模式配置为单例
builder.Services.AddSingleton(startupConfig);

// ============================================================================
// 配置 Web API 支持
// ============================================================================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // 配置 API 元信息
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "窄带分流器分拣系统 API",
        Version = "v1",
        Description = "窄带分流器分拣系统 RESTful API 文档，提供主线控制、包裹管理、格口配置、仿真控制等功能"
    });

    // 引入 XML 文档注释
    var hostXmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var hostXmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, hostXmlFile);
    if (System.IO.File.Exists(hostXmlPath))
    {
        options.IncludeXmlComments(hostXmlPath);
    }

    // 引入 Host.Contracts XML 文档注释
    var contractsXmlFile = "ZakYip.NarrowBeltDiverterSorter.Host.Contracts.xml";
    var contractsXmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, contractsXmlFile);
    if (System.IO.File.Exists(contractsXmlPath))
    {
        options.IncludeXmlComments(contractsXmlPath);
    }
});

// ============================================================================
// 配置 SignalR
// ============================================================================
builder.Services.AddSignalR();

// ============================================================================
// 配置选项
// ============================================================================

// 注册统一配置中心基础设施
builder.Services.AddSingleton<ZakYip.NarrowBeltDiverterSorter.Core.Configuration.IConfigurationDefaultsProvider, 
    ZakYip.NarrowBeltDiverterSorter.Core.Configuration.ConfigurationDefaultsProvider>();
builder.Services.AddSingleton<ZakYip.NarrowBeltDiverterSorter.Infrastructure.Configuration.IAppConfigurationStore, 
    ZakYip.NarrowBeltDiverterSorter.Infrastructure.Configuration.LiteDbAppConfigurationStore>();
builder.Services.AddSingleton<ZakYip.NarrowBeltDiverterSorter.Host.Configuration.IHostConfigurationProvider, 
    ZakYip.NarrowBeltDiverterSorter.Host.Configuration.HostConfigurationProvider>();

// 注册 Sorter 配置提供器（用于主线驱动选择和 Rema 连接参数）
builder.Services.AddSingleton<ZakYip.NarrowBeltDiverterSorter.Core.Configuration.ISorterConfigurationProvider,
    ZakYip.NarrowBeltDiverterSorter.Infrastructure.Configuration.SorterConfigurationProvider>();

// 注册配置仓储（保留用于向后兼容和特定仓储需求）
builder.Services.AddSingleton<IMainLineOptionsRepository, LiteDbMainLineOptionsRepository>();
builder.Services.AddSingleton<IInfeedLayoutOptionsRepository, LiteDbInfeedLayoutOptionsRepository>();
builder.Services.AddSingleton<IChuteConfigRepository, LiteDbChuteConfigRepository>();
builder.Services.AddSingleton<IUpstreamConnectionOptionsRepository, LiteDbUpstreamConnectionOptionsRepository>();
builder.Services.AddSingleton<ILongRunLoadTestOptionsRepository, LiteDbLongRunLoadTestOptionsRepository>();
builder.Services.AddSingleton<IFeedingCapacityOptionsRepository, LiteDbFeedingCapacityOptionsRepository>();

// 从统一配置提供器加载主线控制选项
builder.Services.AddSingleton<IOptions<MainLineControlOptions>>(sp =>
{
    var provider = sp.GetRequiredService<ZakYip.NarrowBeltDiverterSorter.Host.Configuration.IHostConfigurationProvider>();
    var options = provider.GetMainLineControlOptionsAsync().GetAwaiter().GetResult();
    return Options.Create(options);
});

// 从统一配置提供器加载入口布局选项  
builder.Services.AddSingleton<IOptions<InfeedLayoutOptions>>(sp =>
{
    var provider = sp.GetRequiredService<ZakYip.NarrowBeltDiverterSorter.Host.Configuration.IHostConfigurationProvider>();
    var options = provider.GetInfeedLayoutOptionsAsync().GetAwaiter().GetResult();
    return Options.Create(options);
});

// 注册 InfeedLayoutOptions 为单例（ParcelLoadPlanner 需要直接注入）
builder.Services.AddSingleton(sp =>
{
    var provider = sp.GetRequiredService<ZakYip.NarrowBeltDiverterSorter.Host.Configuration.IHostConfigurationProvider>();
    return provider.GetInfeedLayoutOptionsAsync().GetAwaiter().GetResult();
});

// ========================================================================
// 以下配置已迁移到 LiteDB 统一配置中心，通过 IHostConfigurationProvider 访问
// 这些 Configure<T> 调用已被注释，以避免与 LiteDB 配置冲突
// ========================================================================

// 配置格口IO监视器选项 (TODO: 待迁移到 LiteDB)
builder.Services.Configure<ChuteIoMonitorConfiguration>(
    builder.Configuration.GetSection("ChuteIoMonitor"));

// 配置小车参数寄存器 (TODO: 待迁移到 LiteDB)
builder.Services.Configure<CartParameterRegisterConfiguration>(
    builder.Configuration.GetSection("CartParameterRegisters"));

// 注册 CartParameterRegisterConfiguration 为单例（CartParameterDriver 需要直接注入）
builder.Services.AddSingleton(sp =>
{
    var config = new CartParameterRegisterConfiguration();
    builder.Configuration.GetSection("CartParameterRegisters").Bind(config);
    return config;
});

// 配置格口映射 (TODO: 待迁移到 LiteDB)
builder.Services.Configure<ChuteMappingConfiguration>(
    builder.Configuration.GetSection("ChuteMapping"));

// 注册 ChuteMappingConfiguration 为单例（ChuteTransmitterDriver 和 ChuteSafetyService 需要直接注入）
builder.Services.AddSingleton(sp =>
{
    var config = new ChuteMappingConfiguration();
    builder.Configuration.GetSection("ChuteMapping").Bind(config);
    return config;
});

// 配置现场总线客户端 (TODO: 待迁移到 LiteDB)
builder.Services.Configure<FieldBusClientConfiguration>(
    builder.Configuration.GetSection("FieldBus"));

// 注册 FieldBusClientConfiguration 为单例（FieldBusClient 需要直接注入）
builder.Services.AddSingleton(sp =>
{
    var config = new FieldBusClientConfiguration();
    builder.Configuration.GetSection("FieldBus").Bind(config);
    return config;
});

// 配置主线驱动实现选项（从 appsettings.json 读取，用于选择驱动类型和串口连接参数）
builder.Services.Configure<MainLineDriveOptions>(
    builder.Configuration.GetSection(MainLineDriveOptions.SectionName));

// 配置格口布局 (TODO: 待迁移到 LiteDB)
builder.Services.Configure<ChuteLayoutProfile>(
    builder.Configuration.GetSection("ChuteLayout"));

// 配置目标格口分配策略 (TODO: 待迁移到 LiteDB)
builder.Services.Configure<TargetChuteAssignmentProfile>(
    builder.Configuration.GetSection("TargetChuteAssignment"));

// 注意：以下配置已迁移到 LiteDB，通过 IHostConfigurationProvider 访问
// - RemaLm1000HOptions -> 通过 GetRemaLm1000HConfigurationAsync()
// - ChuteIoOptions -> 通过 GetChuteIoConfigurationAsync()
// - NarrowBeltSimulationOptions -> 通过 GetSimulationOptionsAsync()
// - LiveViewPushOptions -> 通过 GetSignalRPushConfigurationAsync()
// - SafetyConfiguration -> 通过 GetSafetyConfigurationAsync()
// - RecordingConfiguration -> 通过 GetRecordingConfigurationAsync()


// ============================================================================
// 注册事件总线 (Observability)
// ============================================================================

builder.Services.AddSingleton<IEventBus, InMemoryEventBus>();

// ============================================================================
// 注册事件录制与回放 (Observability)
// ============================================================================

// 注册文件事件录制管理器（同时实现管理器和录制器接口）
builder.Services.AddSingleton<FileEventRecordingManager>();
builder.Services.AddSingleton<IEventRecordingManager>(sp => sp.GetRequiredService<FileEventRecordingManager>());
builder.Services.AddSingleton<IEventRecorder>(sp => sp.GetRequiredService<FileEventRecordingManager>());

// 注册录制事件订阅器（作为托管服务自动启动）
builder.Services.AddSingleton<RecordingEventSubscriber>();

// ============================================================================
// 注册实时视图聚合器 (Observability)
// ============================================================================

builder.Services.AddSingleton<INarrowBeltLiveView, NarrowBeltLiveView>();

// 注册包裹时间线服务
builder.Services.AddSingleton<ZakYip.NarrowBeltDiverterSorter.Core.Abstractions.IParcelTimelineService>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<ZakYip.NarrowBeltDiverterSorter.Observability.Timeline.ParcelTimelineService>>();
    return new ZakYip.NarrowBeltDiverterSorter.Observability.Timeline.ParcelTimelineService(logger, capacity: 10000);
});

// ============================================================================
// 注册 SignalR 推送桥接服务
// ============================================================================

builder.Services.AddHostedService<LiveViewBridgeService>();

// 注册供包容量监控工作器
builder.Services.AddHostedService<FeedingCapacityMonitorWorker>();

// ============================================================================
// 注册配置存储 (Infrastructure)
// ============================================================================

// 解析 LiteDB 配置并初始化数据库路径
var liteDbSection = builder.Configuration.GetSection("LiteDb");
var liteDbOptions = liteDbSection.Get<ZakYip.NarrowBeltDiverterSorter.Infrastructure.LiteDb.LiteDbOptions>() 
                 ?? new ZakYip.NarrowBeltDiverterSorter.Infrastructure.LiteDb.LiteDbOptions 
                 { 
                     FilePath = "AppData/NarrowBeltConfig.db" 
                 };

// 使用运行目录作为根目录解析为绝对路径
var rootPath = AppContext.BaseDirectory;
var fullPath = Path.GetFullPath(Path.Combine(rootPath, liteDbOptions.FilePath));

// 确保目录存在
var directory = Path.GetDirectoryName(fullPath);
if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
{
    Directory.CreateDirectory(directory);
    Console.WriteLine($"已创建配置数据库目录: {directory}");
}

Console.WriteLine($"已初始化配置数据库: {fullPath}");

// 注册 LiteDbSorterConfigurationStore，使用解析后的绝对路径
builder.Services.AddSingleton<ISorterConfigurationStore>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<LiteDbSorterConfigurationStore>>();
    return new LiteDbSorterConfigurationStore(logger, fullPath);
});

// 注册 IChuteTransmitterConfigurationPort（使用同一个 LiteDbSorterConfigurationStore 实例）
builder.Services.AddSingleton<ZakYip.NarrowBeltDiverterSorter.Core.Abstractions.IChuteTransmitterConfigurationPort>(sp =>
{
    return sp.GetRequiredService<ISorterConfigurationStore>() as LiteDbSorterConfigurationStore
        ?? throw new InvalidOperationException("LiteDbSorterConfigurationStore 未正确注册");
});

// 注册旧的 IConfigStore 用于兼容性（已废弃但某些代码仍在使用）
#pragma warning disable CS0618 // Type or member is obsolete
builder.Services.AddSingleton<ZakYip.NarrowBeltDiverterSorter.Infrastructure.Configuration.IConfigStore, ZakYip.NarrowBeltDiverterSorter.Infrastructure.Configuration.LiteDbConfigStore>();
#pragma warning restore CS0618 // Type or member is obsolete

// ============================================================================
// 注册上游规则引擎端口
// ============================================================================

// 注册工厂和客户端
builder.Services.AddSingleton<ZakYip.NarrowBeltDiverterSorter.Communication.Upstream.SortingRuleEngineClientFactory>();

// 注册 ISortingRuleEngineClient
builder.Services.AddSingleton<ZakYip.NarrowBeltDiverterSorter.Communication.Upstream.ISortingRuleEngineClient>(serviceProvider =>
{
    var configProvider = serviceProvider.GetRequiredService<ZakYip.NarrowBeltDiverterSorter.Host.Configuration.IHostConfigurationProvider>();
    var upstreamOptions = configProvider.GetUpstreamOptionsAsync().GetAwaiter().GetResult();
    
    var factory = serviceProvider.GetRequiredService<ZakYip.NarrowBeltDiverterSorter.Communication.Upstream.SortingRuleEngineClientFactory>();
    var innerClient = factory.CreateClient(upstreamOptions);
    
    // 获取连接地址
    string? connectionAddress = upstreamOptions.Mode switch
    {
        ZakYip.NarrowBeltDiverterSorter.Communication.Upstream.UpstreamMode.Mqtt => 
            upstreamOptions.Mqtt != null ? $"{upstreamOptions.Mqtt.Broker}:{upstreamOptions.Mqtt.Port}" : null,
        ZakYip.NarrowBeltDiverterSorter.Communication.Upstream.UpstreamMode.Tcp => 
            upstreamOptions.Tcp != null ? $"{upstreamOptions.Tcp.Host}:{upstreamOptions.Tcp.Port}" : null,
        _ => null
    };
    
    // 包装为可观察的客户端
    var eventBus = serviceProvider.GetService<ZakYip.NarrowBeltDiverterSorter.Core.Abstractions.IEventBus>();
    var logger = serviceProvider.GetRequiredService<ILoggerFactory>()
        .CreateLogger<ZakYip.NarrowBeltDiverterSorter.Communication.Upstream.ObservableSortingRuleEngineClient>();
    var client = new ZakYip.NarrowBeltDiverterSorter.Communication.Upstream.ObservableSortingRuleEngineClient(
        innerClient, 
        upstreamOptions.Mode.ToString(), 
        connectionAddress,
        eventBus, 
        logger);
    
    // 如果不是 Disabled 模式，尝试连接
    if (upstreamOptions.Mode != ZakYip.NarrowBeltDiverterSorter.Communication.Upstream.UpstreamMode.Disabled)
    {
        _ = client.ConnectAsync().GetAwaiter().GetResult();
    }
    else
    {
        // 对于 Disabled 模式，发布初始状态事件
        if (eventBus != null)
        {
            var eventArgs = new ZakYip.NarrowBeltDiverterSorter.Core.Domain.Upstream.UpstreamRuleEngineStatusChangedEventArgs
            {
                Mode = upstreamOptions.Mode.ToString(),
                Status = ZakYip.NarrowBeltDiverterSorter.Core.Domain.Upstream.UpstreamConnectionStatus.Disabled,
                ConnectionAddress = null
            };
            _ = eventBus.PublishAsync(eventArgs, CancellationToken.None);
        }
    }
    
    return client;
});

// 注册 ISortingRuleEnginePort（通过适配器）
builder.Services.AddSingleton<ZakYip.NarrowBeltDiverterSorter.Core.Domain.Sorting.ISortingRuleEnginePort, 
    ZakYip.NarrowBeltDiverterSorter.Execution.Upstream.SortingRuleEnginePortAdapter>();

// ============================================================================
// 注册现场总线和驱动
// ============================================================================

// 注册现场总线客户端
builder.Services.AddSingleton<IFieldBusClient, FieldBusClient>();

// ============================================================================
// 根据配置注册主线驱动实现
// ============================================================================

// 从 LiteDB 加载 Sorter 配置（如果不存在，则从 appsettings.json 初始化）
var sorterConfigProvider = builder.Services.BuildServiceProvider()
    .GetRequiredService<ZakYip.NarrowBeltDiverterSorter.Core.Configuration.ISorterConfigurationProvider>();
var sorterOptions = sorterConfigProvider.LoadAsync().GetAwaiter().GetResult();

var mainLineMode = sorterOptions.MainLine.Mode;
var remaOptions = sorterOptions.MainLine.Rema;

if (mainLineMode == "Simulation")
{
    // 注册仿真主线驱动和端口
    var fakeMainLineDrive = new FakeMainLineDrivePort();
    var fakeMainLineFeedback = new FakeMainLineFeedbackPort(fakeMainLineDrive);
    
    builder.Services.AddSingleton(fakeMainLineDrive);
    builder.Services.AddSingleton(fakeMainLineFeedback);
    builder.Services.AddSingleton<IMainLineDrivePort>(fakeMainLineDrive);
    builder.Services.AddSingleton<IMainLineFeedbackPort>(fakeMainLineFeedback);
    
    // 注册 SimulatedMainLineDrive 为 IMainLineDrive
    builder.Services.AddSingleton<IMainLineDrive, SimulatedMainLineDrive>();
    
    // 注册标准 MainLineControlService（使用 PID 控制）
    builder.Services.AddSingleton<IMainLineControlService, MainLineControlService>();
    
    Console.WriteLine("主线驱动实现: 仿真主线");
}
else if (mainLineMode == "RemaLm1000H")
{
    // 注册 RemaLm1000HTransport（使用桩实现用于测试）
    builder.Services.AddSingleton<IRemaLm1000HTransport, StubRemaLm1000HTransport>();
    
    // 注册 RemaLm1000HMainLineDrive
    builder.Services.AddSingleton<RemaLm1000HMainLineDrive>();
    
    // 注册 IMainLineDrive（指向 RemaLm1000HMainLineDrive）
    builder.Services.AddSingleton<IMainLineDrive>(sp => sp.GetRequiredService<RemaLm1000HMainLineDrive>());
    
    // 注册 RemaMainLineControlServiceAdapter 作为 IMainLineControlService
    // 用于适配 MainLineControlWorker 的启动/停止流程
    builder.Services.AddSingleton<IMainLineControlService, RemaMainLineControlServiceAdapter>();
    
    // 注册占位符端口用于其他可能的依赖
    builder.Services.AddSingleton<IMainLineDrivePort, StubMainLineDrivePort>();
    builder.Services.AddSingleton<IMainLineFeedbackPort, StubMainLineFeedbackPort>();
    
    Console.WriteLine("主线驱动实现: Rema LM1000H");
    
    // 输出雷马连接参数（便于现场排查）
    if (remaOptions != null)
    {
        Console.WriteLine($"  串口号: {remaOptions.PortName}");
        Console.WriteLine($"  波特率: {remaOptions.BaudRate}");
        Console.WriteLine($"  数据位: {remaOptions.DataBits}");
        Console.WriteLine($"  奇偶校验: {remaOptions.Parity}");
        Console.WriteLine($"  停止位: {remaOptions.StopBits}");
        Console.WriteLine($"  站号: {remaOptions.SlaveAddress}");
        Console.WriteLine($"  读取超时: {remaOptions.ReadTimeout.TotalMilliseconds} ms");
        Console.WriteLine($"  写入超时: {remaOptions.WriteTimeout.TotalMilliseconds} ms");
        Console.WriteLine($"  最大重试: {remaOptions.MaxRetries}");
    }
}
else
{
    throw new InvalidOperationException(
        $"不支持的主线驱动实现类型: {mainLineMode}");
}

// 注册小车参数驱动
builder.Services.AddSingleton<ICartParameterPort, CartParameterDriver>();

// 注册格口发信器驱动（底层实现，由 SortingExecutionWorker 和 ChuteSafetyService 使用）
// 注意：IChuteTransmitterPort 是底层硬件端口抽象，用于现有代码兼容性
// 新代码应优先使用 IChuteIoService，它提供了更通用的多 IP 端点支持
builder.Services.AddSingleton<IChuteTransmitterPort, ChuteTransmitterDriver>();

// ============================================================================
// 根据配置注册格口 IO 服务实现
// ============================================================================
// IChuteIoService 是新的通用格口 IO 服务接口，支持：
// 1. 多个 IP 端点（每个端点可以是不同的品牌/型号）
// 2. 灵活的通道映射配置（避免硬编码魔法数字）
// 3. 品牌无关的抽象（不暴露具体硬件细节）
// ============================================================================

var chuteIoOptions = builder.Configuration
    .GetSection(ChuteIoOptions.SectionName)
    .Get<ChuteIoOptions>();

if (chuteIoOptions != null && chuteIoOptions.Mode == "Simulation")
{
    // 创建端点列表和映射
    var endpoints = new List<IChuteIoEndpoint>();
    var chuteMapping = new Dictionary<long, (IChuteIoEndpoint endpoint, int channelIndex)>();

    foreach (var nodeConfig in chuteIoOptions.Nodes)
    {
        // 创建模拟端点
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var endpointLogger = loggerFactory.CreateLogger<SimulationChuteIoEndpoint>();
        var endpoint = new SimulationChuteIoEndpoint(
            nodeConfig.NodeKey,
            nodeConfig.MaxChannelCount,
            endpointLogger);

        endpoints.Add(endpoint);

        // 构建映射关系
        foreach (var channelBinding in nodeConfig.Channels)
        {
            chuteMapping[channelBinding.ChuteId] = (endpoint, channelBinding.ChannelIndex);
        }
    }

    // 注册模拟格口 IO 服务
    builder.Services.AddSingleton<IChuteIoService>(sp =>
    {
        var logger = sp.GetRequiredService<ILogger<SimulationChuteIoService>>();
        return new SimulationChuteIoService(endpoints, chuteMapping, logger);
    });

    Console.WriteLine("格口 IO 实现: 模拟模式");
    Console.WriteLine($"  节点数量: {chuteIoOptions.Nodes.Count}");
    foreach (var node in chuteIoOptions.Nodes)
    {
        Console.WriteLine($"  - {node.NodeKey}: {node.Channels.Count} 个通道绑定");
    }
}
else if (chuteIoOptions != null && chuteIoOptions.Mode == "ZhiQian32Relay")
{
    // 创建智嵌继电器端点列表和映射
    var zhiqianEndpoints = new List<ZhiQian32RelayEndpoint>();
    var chuteMapping = new Dictionary<long, (IChuteIoEndpoint endpoint, int channelIndex)>();

    foreach (var nodeConfig in chuteIoOptions.Nodes)
    {
        // 仅处理品牌为 ZhiQian32Relay 的节点
        if (nodeConfig.Brand != "ZhiQian32Relay")
        {
            Console.WriteLine($"  警告: 节点 {nodeConfig.NodeKey} 的品牌 '{nodeConfig.Brand}' 与模式 'ZhiQian32Relay' 不匹配，跳过");
            continue;
        }

        // 创建智嵌继电器端点
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var endpointLogger = loggerFactory.CreateLogger<ZhiQian32RelayEndpoint>();
        var clientLogger = loggerFactory.CreateLogger<ZhiQian32RelayClient>();
        
        var endpoint = new ZhiQian32RelayEndpoint(
            nodeConfig.NodeKey,
            nodeConfig.IpAddress,
            nodeConfig.Port,
            nodeConfig.MaxChannelCount,
            endpointLogger,
            clientLogger);

        zhiqianEndpoints.Add(endpoint);

        // 构建映射关系
        foreach (var channelBinding in nodeConfig.Channels)
        {
            chuteMapping[channelBinding.ChuteId] = (endpoint, channelBinding.ChannelIndex);
        }
    }

    // 注册智嵌继电器格口 IO 服务
    builder.Services.AddSingleton<IChuteIoService>(sp =>
    {
        var logger = sp.GetRequiredService<ILogger<ZhiQian32RelayChuteIoService>>();
        return new ZhiQian32RelayChuteIoService(zhiqianEndpoints, chuteMapping, logger);
    });

    Console.WriteLine("格口 IO 实现: 智嵌32路网络继电器");
    Console.WriteLine($"  节点数量: {zhiqianEndpoints.Count}");
    foreach (var node in chuteIoOptions.Nodes.Where(n => n.Brand == "ZhiQian32Relay"))
    {
        Console.WriteLine($"  - {node.NodeKey}: {node.IpAddress}:{node.Port}, {node.Channels.Count} 个通道绑定");
    }
}
else if (chuteIoOptions == null)
{
    Console.WriteLine("格口 IO 配置未找到，跳过注册");
}
else
{
    Console.WriteLine($"格口 IO 模式 '{chuteIoOptions.Mode}' 尚未实现，跳过注册");
}

// 注册传感器端口（这些需要具体实现，这里使用占位符）
// TODO: 实现具体的传感器端口
// builder.Services.AddSingleton<IOriginSensorPort, OriginSensorPortImplementation>();
// builder.Services.AddSingleton<IInfeedSensorPort, InfeedSensorPortImplementation>();
builder.Services.AddSingleton<IInfeedConveyorPort, StubInfeedConveyorPort>();

// ============================================================================
// 注册领域服务
// ============================================================================

// 注册格口配置提供者（从数据库加载）
builder.Services.AddSingleton<IChuteConfigProvider, RepositoryBackedChuteConfigProvider>();

// 注册小车环构建器
builder.Services.AddSingleton<ICartRingBuilder, CartRingBuilder>();

// 注册小车位置跟踪器
builder.Services.AddSingleton<ICartPositionTracker, CartPositionTracker>();

// 注册系统运行状态服务
builder.Services.AddSingleton<ZakYip.NarrowBeltDiverterSorter.Core.Domain.SystemState.ISystemRunStateService, 
    ZakYip.NarrowBeltDiverterSorter.Core.Domain.SystemState.SystemRunStateService>();

// 注册系统故障服务
builder.Services.AddSingleton<ZakYip.NarrowBeltDiverterSorter.Core.Domain.SystemState.ISystemFaultService,
    ZakYip.NarrowBeltDiverterSorter.Core.Domain.SystemState.SystemFaultService>();

// 注册包裹生命周期服务（单例，内存存储）
builder.Services.AddSingleton<IParcelLifecycleService, ParcelLifecycleService>();

// 注册包裹生命周期追踪器（用于可观测性）
builder.Services.AddSingleton<IParcelLifecycleTracker, ParcelLifecycleTracker>();

// 注册供包背压控制器
builder.Services.AddSingleton<IFeedingBackpressureController, ZakYip.NarrowBeltDiverterSorter.Execution.Feeding.FeedingBackpressureController>();

// 注册小车生命周期服务
builder.Services.AddSingleton<ICartLifecycleService, CartLifecycleService>();

// 注册包裹装载计划器
builder.Services.AddSingleton<IParcelLoadPlanner, ParcelLoadPlanner>();

// 注册分拣规划器选项（使用默认值）
builder.Services.AddSingleton(sp => new SortingPlannerOptions
{
    CartSpacingMm = 500m // 默认小车间距 500mm
});

// 注册分拣计划器
builder.Services.AddSingleton<ISortingPlanner, SortingPlanner>();

// 注册主线设定点提供者（生产环境）
builder.Services.AddSingleton<IMainLineSetpointProvider, ProductionMainLineSetpointProvider>();

// 注册主线速度提供者和稳定性提供者
// 注意：IMainLineControlService 已在主线驱动配置中根据实现类型注册
builder.Services.AddSingleton<IMainLineSpeedProvider, MainLineSpeedProvider>();
builder.Services.AddSingleton<IMainLineStabilityProvider, MainLineStabilityProvider>();

// 注册格口安全控制服务
builder.Services.AddSingleton<IChuteSafetyService, ChuteSafetyService>();

// ============================================================================
// 注册执行运行时（Execution Runtime）
// ============================================================================

// 注册主线控制运行时
builder.Services.AddSingleton<ZakYip.NarrowBeltDiverterSorter.Core.Domain.Runtime.IMainLineRuntime, 
    ZakYip.NarrowBeltDiverterSorter.Execution.Runtime.MainLineRuntime>();

// 注册包裹路由运行时
builder.Services.AddSingleton<ZakYip.NarrowBeltDiverterSorter.Core.Domain.Runtime.IParcelRoutingRuntime, 
    ZakYip.NarrowBeltDiverterSorter.Execution.Runtime.ParcelRoutingRuntime>();

// 注册安全控制运行时
builder.Services.AddSingleton<ZakYip.NarrowBeltDiverterSorter.Core.Domain.Runtime.ISafetyRuntime, 
    ZakYip.NarrowBeltDiverterSorter.Execution.Runtime.SafetyRuntime>();

// ============================================================================
// 注册安全编排器和安全输入监控器
// ============================================================================

// 注册安全输入监控器
// 使用 Execution 层的模拟版本（适用于仿真和测试）
builder.Services.AddSingleton<ZakYip.NarrowBeltDiverterSorter.Core.Domain.Safety.ISafetyInputMonitor, 
    ZakYip.NarrowBeltDiverterSorter.Execution.Safety.SimulatedSafetyInputMonitor>();

// TODO: 在生产模式下使用真实的安全输入监控器（待实现）
// if (!startupConfig.SimulationMode)
// {
//     builder.Services.AddSingleton<ISafetyInputMonitor, ProductionSafetyInputMonitor>();
// }

// 注册安全编排器
builder.Services.AddSingleton<ZakYip.NarrowBeltDiverterSorter.Core.Domain.Safety.ILineSafetyOrchestrator, 
    ZakYip.NarrowBeltDiverterSorter.Execution.Safety.LineSafetyOrchestrator>();

// ============================================================================
// 注册包裹分拣编排器和分拣结果处理器
// ============================================================================

builder.Services.AddSingleton<ZakYip.NarrowBeltDiverterSorter.Execution.Orchestration.ParcelSortingOrchestrator>();
builder.Services.AddSingleton<ZakYip.NarrowBeltDiverterSorter.Execution.Orchestration.SortingResultHandler>();

// ============================================================================
// 注册面板按钮监控和 IO 联动
// ============================================================================

// 注册面板按钮配置
builder.Services.AddSingleton(sp => ZakYip.NarrowBeltDiverterSorter.Core.Configuration.PanelButtonConfiguration.CreateDefault());

// 注册面板 IO 联动选项
builder.Services.AddSingleton(sp => 
{
    var options = new ZakYip.NarrowBeltDiverterSorter.Execution.Panel.PanelIoLinkageOptions
    {
        StartFollowOutputChannels = new List<int>(), // 可根据需要配置
        StopFollowOutputChannels = new List<int>()   // 可根据需要配置
    };
    return Microsoft.Extensions.Options.Options.Create(options);
});

// 注册面板 IO 协调器
builder.Services.AddSingleton<ZakYip.NarrowBeltDiverterSorter.Core.Abstractions.IPanelIoCoordinator, 
    ZakYip.NarrowBeltDiverterSorter.Execution.Panel.PanelIoCoordinator>();

// 注册面板按钮监控器
builder.Services.AddSingleton<ZakYip.NarrowBeltDiverterSorter.Ingress.Safety.PanelButtonMonitor>();

// ============================================================================
// 注册健康检查
// ============================================================================

builder.Services.AddHealthChecks()
    .AddCheck<SystemHealthCheck>("system");

// ============================================================================
// 注册仿真服务（可选，仅在仿真模式下可用）
// ============================================================================
// Note: Commented out due to circular dependency between Host and Simulation projects
/*
// 注册仿真报告服务
builder.Services.AddSingleton<INarrowBeltSimulationReportService, InMemoryNarrowBeltSimulationReportService>();

// 注册场景运行器（可选，如果依赖的服务不可用则不注册）
// 场景运行器需要仿真驱动和时间线记录器
if (mainLineDriveOptions.Implementation == MainLineDriveImplementation.Simulation)
{
    // 注册时间线记录器
    builder.Services.AddSingleton<ParcelTimelineRecorder>();
    
    // 注册场景运行器
    builder.Services.AddTransient<INarrowBeltSimulationScenarioRunner, NarrowBeltSimulationScenarioRunner>();
    
    Console.WriteLine("仿真场景运行器已启用");
}
else
{
    Console.WriteLine("仿真场景运行器已禁用（需要仿真主线驱动）");
}
*/

// ============================================================================
// 根据启动模式注册后台工作器
// ============================================================================

// 主线控制工作器（所有模式都需要）
if (startupConfig.ShouldStartMainLineControl())
{
    builder.Services.AddHostedService<MainLineControlWorker>();
}

// 原点传感器监视器（所有模式都需要）
if (startupConfig.ShouldStartOriginSensorMonitor())
{
    // builder.Services.AddHostedService<OriginSensorMonitorWorker>();
    // TODO: 当 IOriginSensorPort 实现后启用
}

// 入口传感器监视器（bringup-infeed 及以上模式）
if (startupConfig.ShouldStartInfeedSensorMonitor())
{
    // builder.Services.AddHostedService<InfeedSensorMonitorWorker>();
    // TODO: 当 IInfeedSensorPort 实现后启用
}

// 包裹装载协调器（bringup-infeed 及以上模式）
if (startupConfig.ShouldStartParcelLoadCoordinator())
{
    builder.Services.AddHostedService<ParcelLoadCoordinatorWorker>();
}

// 分拣执行工作器（bringup-chutes 及以上模式）
if (startupConfig.ShouldStartSortingExecutionWorker())
{
    builder.Services.AddHostedService<SortingExecutionWorker>();
}

// 格口IO监视器（bringup-chutes 及以上模式）
if (startupConfig.ShouldStartChuteIoMonitor())
{
    // builder.Services.AddHostedService<ChuteIoMonitorWorker>();
    // TODO: 当 FieldBusClient 实现后启用
}

// 包裹路由工作器（仅 normal 模式，上游相关）
if (startupConfig.ShouldStartParcelRoutingWorker())
{
    builder.Services.AddHostedService<ParcelRoutingWorker>();
}

// 上游 Bring-up 诊断工作器（仅 bringup-upstream 模式）
if (startupConfig.ShouldStartUpstreamBringupWorker())
{
    builder.Services.AddHostedService<UpstreamBringupWorker>();
}

// 注册占位符工作器（可以移除）
builder.Services.AddHostedService<Worker>();

// 面板按钮监控工作器（所有模式都需要）
builder.Services.AddHostedService<PanelButtonMonitorWorker>();

// 注册安全控制工作器（确保最早启动，最晚停止）
builder.Services.AddHostedService<SafetyControlWorker>();

var app = builder.Build();

// ============================================================================
// 配置 HTTP 请求管道
// ============================================================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

// ============================================================================
// 配置 SignalR Hub
// ============================================================================
app.MapHub<NarrowBeltLiveHub>("/hubs/narrowbelt-live");

// 输出启动信息
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("=== 系统启动模式: {Mode} ===", startupConfig.GetModeDescription());
logger.LogInformation("主线驱动实现: {Implementation}", mainLineMode);

// 输出雷马连接参数（当使用 RemaLm1000H 模式时）
if (mainLineMode == "RemaLm1000H" && remaOptions != null)
{
    logger.LogInformation("雷马 LM1000H 连接参数:");
    logger.LogInformation("  串口号: {PortName}", remaOptions.PortName);
    logger.LogInformation("  波特率: {BaudRate}", remaOptions.BaudRate);
    logger.LogInformation("  数据位: {DataBits}", remaOptions.DataBits);
    logger.LogInformation("  奇偶校验: {Parity}", remaOptions.Parity);
    logger.LogInformation("  停止位: {StopBits}", remaOptions.StopBits);
    logger.LogInformation("  站号: {SlaveAddress}", remaOptions.SlaveAddress);
    logger.LogInformation("  读取超时: {ReadTimeout} ms", remaOptions.ReadTimeout.TotalMilliseconds);
    logger.LogInformation("  写入超时: {WriteTimeout} ms", remaOptions.WriteTimeout.TotalMilliseconds);
    logger.LogInformation("  最大重试: {MaxRetries}", remaOptions.MaxRetries);
}

logger.LogInformation("已启动服务:");
if (startupConfig.ShouldStartMainLineControl())
    logger.LogInformation("  - 主线控制工作器 (MainLineControlWorker)");
if (startupConfig.ShouldStartOriginSensorMonitor())
    logger.LogInformation("  - 原点传感器监控 (OriginSensorMonitor) [待实现]");
if (startupConfig.ShouldStartInfeedSensorMonitor())
    logger.LogInformation("  - 入口传感器监控 (InfeedSensorMonitor) [待实现]");
if (startupConfig.ShouldStartParcelLoadCoordinator())
    logger.LogInformation("  - 包裹装载协调器 (ParcelLoadCoordinator)");
if (startupConfig.ShouldStartSortingExecutionWorker())
    logger.LogInformation("  - 分拣执行工作器 (SortingExecutionWorker)");
if (startupConfig.ShouldStartChuteIoMonitor())
    logger.LogInformation("  - 格口IO监视器 (ChuteIoMonitor) [待实现]");
if (startupConfig.ShouldStartParcelRoutingWorker())
    logger.LogInformation("  - 包裹路由工作器 (ParcelRoutingWorker)");

logger.LogInformation("Web API 已启用，Swagger UI: /swagger");
logger.LogInformation("SignalR Hub 已启用，Hub 端点: /hubs/narrowbelt-live");

// ============================================================================
// 启动时加载格口 IO 配置并注册到 IChuteTransmitterPort
// ============================================================================
await InitializeChuteIoConfigurationsAsync(app.Services, logger);

app.Run();

// ============================================================================
// 辅助方法：加载格口 IO 配置并注册
// ============================================================================
static async Task InitializeChuteIoConfigurationsAsync(IServiceProvider services, ILogger logger)
{
    try
    {
        // 获取配置端口和发信器端口
        var chuteConfigPort = services.GetService<ZakYip.NarrowBeltDiverterSorter.Core.Abstractions.IChuteTransmitterConfigurationPort>();
        var chuteTransmitterPort = services.GetService<IChuteTransmitterPort>();

        if (chuteConfigPort == null || chuteTransmitterPort == null)
        {
            logger.LogWarning("格口 IO 配置端口或发信器端口未注册，跳过格口配置加载");
            return;
        }

        // 从 LiteDB 加载格口 IO 配置
        var bindings = await chuteConfigPort.GetAllBindingsAsync();

        if (bindings.Count == 0)
        {
            logger.LogWarning("格口 IO 配置为空，将无法进行真实分拣，仅能进行主线与小车仿真。");
        }
        else
        {
            logger.LogInformation("加载到 {Count} 条格口 IO 配置，将注册到 IChuteTransmitterPort。", bindings.Count);

            // 如果 ChuteTransmitterDriver 有 RegisterBindings 方法，调用它
            if (chuteTransmitterPort is ZakYip.NarrowBeltDiverterSorter.Execution.Chute.ChuteTransmitterDriver driver)
            {
                driver.RegisterBindings(bindings);
                logger.LogInformation("已成功注册 {Count} 条格口 IO 配置到 ChuteTransmitterDriver。", bindings.Count);
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "加载格口 IO 配置时发生异常");
    }
}

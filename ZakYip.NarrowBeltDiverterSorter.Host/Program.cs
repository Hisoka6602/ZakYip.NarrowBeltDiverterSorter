using ZakYip.NarrowBeltDiverterSorter.Communication;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Application;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Parcels;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.MainLine;
using ZakYip.NarrowBeltDiverterSorter.Drivers.MainLine;
using ZakYip.NarrowBeltDiverterSorter.Execution.MainLine;
using ZakYip.NarrowBeltDiverterSorter.Host;

var builder = Host.CreateApplicationBuilder(args);

// 配置上游分拣系统API选项
builder.Services.Configure<UpstreamSortingApiOptions>(
    builder.Configuration.GetSection(UpstreamSortingApiOptions.SectionName));

// 配置主线控制选项
builder.Services.Configure<MainLineControlOptions>(
    builder.Configuration.GetSection("MainLineControl"));

// 注册HttpClient for UpstreamSortingApiClient
builder.Services.AddHttpClient<IUpstreamSortingApiClient, UpstreamSortingApiClient>((serviceProvider, client) =>
{
    var options = builder.Configuration
        .GetSection(UpstreamSortingApiOptions.SectionName)
        .Get<UpstreamSortingApiOptions>() ?? new UpstreamSortingApiOptions();
    
    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
});

// 注册包裹生命周期服务（单例，内存存储）
builder.Services.AddSingleton<IParcelLifecycleService, ParcelLifecycleService>();

// 注册主线驱动和反馈端口（虚拟实现，单例）
builder.Services.AddSingleton<RemaMainLineDrive>();
builder.Services.AddSingleton<IMainLineDrivePort>(sp => sp.GetRequiredService<RemaMainLineDrive>());
builder.Services.AddSingleton<IMainLineFeedbackPort>(sp => sp.GetRequiredService<RemaMainLineDrive>());

// 注册主线控制服务和速度提供者
builder.Services.AddSingleton<IMainLineControlService, MainLineControlService>();
builder.Services.AddSingleton<IMainLineSpeedProvider, MainLineSpeedProvider>();

// 注册后台工作器
builder.Services.AddHostedService<Worker>();
builder.Services.AddHostedService<ParcelRoutingWorker>();
builder.Services.AddHostedService<MainLineControlWorker>();

var host = builder.Build();
host.Run();

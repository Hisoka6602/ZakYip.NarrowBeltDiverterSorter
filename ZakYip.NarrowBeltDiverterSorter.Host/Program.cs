using ZakYip.NarrowBeltDiverterSorter.Communication;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Application;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Parcels;
using ZakYip.NarrowBeltDiverterSorter.Host;

var builder = Host.CreateApplicationBuilder(args);

// 配置上游分拣系统API选项
builder.Services.Configure<UpstreamSortingApiOptions>(
    builder.Configuration.GetSection(UpstreamSortingApiOptions.SectionName));

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

// 注册后台工作器
builder.Services.AddHostedService<Worker>();
builder.Services.AddHostedService<ParcelRoutingWorker>();

var host = builder.Build();
host.Run();

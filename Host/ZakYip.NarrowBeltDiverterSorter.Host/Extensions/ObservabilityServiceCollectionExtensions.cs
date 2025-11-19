using Microsoft.Extensions.DependencyInjection;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Observability;
using ZakYip.NarrowBeltDiverterSorter.Observability.LiveView;
using ZakYip.NarrowBeltDiverterSorter.Observability.Recording;
using ZakYip.NarrowBeltDiverterSorter.Observability.Timeline;

namespace ZakYip.NarrowBeltDiverterSorter.Host.Extensions;

/// <summary>
/// Observability 层依赖注入扩展方法
/// </summary>
public static class ObservabilityServiceCollectionExtensions
{
    /// <summary>
    /// 注册 Observability 层服务
    /// </summary>
    public static IServiceCollection AddObservability(this IServiceCollection services)
    {
        // 注册事件总线
        services.AddSingleton<IEventBus, InMemoryEventBus>();

        // 注册文件事件录制管理器（同时实现管理器和录制器接口）
        services.AddSingleton<FileEventRecordingManager>();
        services.AddSingleton<IEventRecordingManager>(sp => sp.GetRequiredService<FileEventRecordingManager>());
        services.AddSingleton<IEventRecorder>(sp => sp.GetRequiredService<FileEventRecordingManager>());

        // 注册录制事件订阅器（作为托管服务自动启动）
        services.AddSingleton<RecordingEventSubscriber>();

        // 注册实时视图聚合器
        services.AddSingleton<INarrowBeltLiveView, NarrowBeltLiveView>();

        // 注册包裹时间线服务
        services.AddSingleton<IParcelTimelineService>(sp =>
        {
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ParcelTimelineService>>();
            return new ParcelTimelineService(logger, capacity: 10000);
        });

        return services;
    }
}

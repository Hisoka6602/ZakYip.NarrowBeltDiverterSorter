using ZakYip.NarrowBeltDiverterSorter.Communication.Upstream;
using ZakYip.NarrowBeltDiverterSorter.Core.Configuration;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Feeding;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.MainLine;

namespace ZakYip.NarrowBeltDiverterSorter.Host.Configuration;

/// <summary>
/// Host 配置提供器接口
/// 统一管理所有运行时配置，提供"默认值 + LiteDB 覆盖"的组合逻辑
/// 确保无论 LiteDB 是否可用，Host 都能正常启动
/// </summary>
public interface IHostConfigurationProvider
{
    /// <summary>
    /// 获取主线控制配置
    /// </summary>
    Task<MainLineControlOptions> GetMainLineControlOptionsAsync(CancellationToken ct = default);
    
    /// <summary>
    /// 获取入口布局配置
    /// </summary>
    Task<InfeedLayoutOptions> GetInfeedLayoutOptionsAsync(CancellationToken ct = default);
    
    /// <summary>
    /// 获取上游连接配置
    /// </summary>
    Task<UpstreamConnectionOptions> GetUpstreamConnectionOptionsAsync(CancellationToken ct = default);
    
    /// <summary>
    /// 获取仿真配置
    /// </summary>
    Task<NarrowBeltSimulationOptions> GetSimulationOptionsAsync(CancellationToken ct = default);
    
    /// <summary>
    /// 获取安全配置
    /// </summary>
    Task<SafetyConfiguration> GetSafetyConfigurationAsync(CancellationToken ct = default);
    
    /// <summary>
    /// 获取录制配置
    /// </summary>
    Task<RecordingConfiguration> GetRecordingConfigurationAsync(CancellationToken ct = default);
    
    /// <summary>
    /// 获取 SignalR 推送配置
    /// </summary>
    Task<SignalRPushConfiguration> GetSignalRPushConfigurationAsync(CancellationToken ct = default);
    
    /// <summary>
    /// 获取 Rema LM1000H 配置
    /// </summary>
    Task<RemaLm1000HConfiguration> GetRemaLm1000HConfigurationAsync(CancellationToken ct = default);
    
    /// <summary>
    /// 获取格口 IO 配置
    /// </summary>
    Task<ChuteIoConfiguration> GetChuteIoConfigurationAsync(CancellationToken ct = default);
    
    /// <summary>
    /// 获取长跑测试配置
    /// </summary>
    Task<LongRunLoadTestOptions> GetLongRunLoadTestOptionsAsync(CancellationToken ct = default);
}

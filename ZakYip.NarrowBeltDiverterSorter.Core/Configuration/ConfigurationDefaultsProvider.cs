using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Feeding;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.MainLine;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Configuration;

/// <summary>
/// 配置默认值提供器实现
/// 为各种配置类型提供统一的默认值
/// </summary>
public sealed class ConfigurationDefaultsProvider : IConfigurationDefaultsProvider
{
    /// <inheritdoc/>
    public T GetDefaults<T>() where T : class
    {
        var type = typeof(T);
        
        // 主线控制配置
        if (type == typeof(MainLineControlOptions))
        {
            return (MainLineControlOptions.CreateDefault() as T)!;
        }
        
        // 入口布局配置
        if (type == typeof(InfeedLayoutOptions))
        {
            return (InfeedLayoutOptions.CreateDefault() as T)!;
        }
        
        // 仿真配置
        if (type == typeof(NarrowBeltSimulationOptions))
        {
            return (NarrowBeltSimulationOptions.CreateDefault() as T)!;
        }
        
        // 录制配置
        if (type == typeof(RecordingConfiguration))
        {
            return (RecordingConfiguration.CreateDefault() as T)!;
        }
        
        // 安全配置
        if (type == typeof(SafetyConfiguration))
        {
            return (SafetyConfiguration.CreateDefault() as T)!;
        }
        
        // SignalR 推送配置
        if (type == typeof(SignalRPushConfiguration))
        {
            return (SignalRPushConfiguration.CreateDefault() as T)!;
        }
        
        // Rema LM1000H 配置
        if (type == typeof(RemaLm1000HConfiguration))
        {
            return (RemaLm1000HConfiguration.CreateDefault() as T)!;
        }
        
        // 格口 IO 配置
        if (type == typeof(ChuteIoConfiguration))
        {
            return (ChuteIoConfiguration.CreateDefault() as T)!;
        }
        
        // 长跑测试配置
        if (type == typeof(LongRunLoadTestOptions))
        {
            return (LongRunLoadTestOptions.CreateDefault() as T)!;
        }
        
        // 默认：抛出异常，因为没有注册的默认值提供器
        throw new NotSupportedException($"配置类型 {type.FullName} 没有注册默认值提供器");
    }
}

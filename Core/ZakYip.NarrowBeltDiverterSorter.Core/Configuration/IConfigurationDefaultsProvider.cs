namespace ZakYip.NarrowBeltDiverterSorter.Core.Configuration;

/// <summary>
/// 配置默认值提供器接口
/// 用于为所有配置类型提供统一的默认值
/// </summary>
public interface IConfigurationDefaultsProvider
{
    /// <summary>
    /// 获取指定类型的默认配置
    /// </summary>
    /// <typeparam name="T">配置类型</typeparam>
    /// <returns>默认配置实例</returns>
    T GetDefaults<T>() where T : class;
}

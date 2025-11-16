namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.Sorting;

/// <summary>
/// 格口配置提供者接口
/// </summary>
public interface IChuteConfigProvider
{
    /// <summary>
    /// 获取所有格口配置
    /// </summary>
    /// <returns>格口配置列表</returns>
    IReadOnlyList<ChuteConfig> GetAllConfigs();

    /// <summary>
    /// 获取指定格口的配置
    /// </summary>
    /// <param name="chuteId">格口ID</param>
    /// <returns>格口配置，如果不存在返回null</returns>
    ChuteConfig? GetConfig(ChuteId chuteId);
}

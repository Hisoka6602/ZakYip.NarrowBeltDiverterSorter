namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.Sorting;

/// <summary>
/// 格口配置集合
/// </summary>
public class ChuteConfigSet
{
    /// <summary>
    /// 格口配置列表
    /// </summary>
    public List<ChuteConfig> Configs { get; set; } = new();

    /// <summary>
    /// 默认配置集
    /// </summary>
    public static ChuteConfigSet CreateDefault()
    {
        return new ChuteConfigSet
        {
            Configs = new List<ChuteConfig>()
        };
    }
}

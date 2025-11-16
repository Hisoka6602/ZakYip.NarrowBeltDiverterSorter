namespace ZakYip.NarrowBeltDiverterSorter.Host;

/// <summary>
/// 上游分拣系统API配置
/// </summary>
public class UpstreamSortingApiOptions
{
    /// <summary>
    /// 配置节名称
    /// </summary>
    public const string SectionName = "UpstreamSortingApi";

    /// <summary>
    /// 上游API基础URL
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:5000";

    /// <summary>
    /// 超时时间（秒）
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
}

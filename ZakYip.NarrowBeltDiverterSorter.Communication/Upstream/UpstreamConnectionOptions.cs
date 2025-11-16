namespace ZakYip.NarrowBeltDiverterSorter.Communication.Upstream;

/// <summary>
/// 上游连接配置选项
/// </summary>
public class UpstreamConnectionOptions
{
    /// <summary>
    /// 基础URL
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:5000";

    /// <summary>
    /// 请求超时时间（秒）
    /// </summary>
    public int RequestTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// 认证Token（如果需要）
    /// </summary>
    public string? AuthToken { get; set; }

    /// <summary>
    /// 创建默认配置
    /// </summary>
    public static UpstreamConnectionOptions CreateDefault()
    {
        return new UpstreamConnectionOptions
        {
            BaseUrl = "http://localhost:5000",
            RequestTimeoutSeconds = 30,
            AuthToken = null
        };
    }
}

namespace ZakYip.NarrowBeltDiverterSorter.Core.Configuration;

/// <summary>
/// 上游路由配置选项
/// 控制上游规则引擎交互的超时和异常处理行为
/// </summary>
public class UpstreamRoutingOptions
{
    /// <summary>
    /// 上游结果超时时间（从发送请求到接收结果的最大等待时间）
    /// 默认 30 秒
    /// </summary>
    public TimeSpan UpstreamResultTtl { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// 异常格口ID（当上游超时或无法分配时使用）
    /// 默认 9999
    /// </summary>
    public long ErrorChuteId { get; set; } = 9999;

    /// <summary>
    /// 创建默认配置
    /// </summary>
    public static UpstreamRoutingOptions CreateDefault()
    {
        return new UpstreamRoutingOptions
        {
            UpstreamResultTtl = TimeSpan.FromSeconds(30),
            ErrorChuteId = 9999
        };
    }
}

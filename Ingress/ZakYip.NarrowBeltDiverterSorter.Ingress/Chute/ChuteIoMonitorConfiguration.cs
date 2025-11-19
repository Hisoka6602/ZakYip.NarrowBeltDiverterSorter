namespace ZakYip.NarrowBeltDiverterSorter.Ingress.Chute;

/// <summary>
/// 格口IO监控配置
/// </summary>
public class ChuteIoMonitorConfiguration
{
    /// <summary>
    /// 轮询间隔（默认50ms）
    /// </summary>
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromMilliseconds(50);

    /// <summary>
    /// 要监控的格口ID列表
    /// </summary>
    public List<long> MonitoredChuteIds { get; set; } = new();

    /// <summary>
    /// 格口IO状态地址映射
    /// Key: ChuteId
    /// Value: DiscreteInputAddress (离散输入地址)
    /// </summary>
    public Dictionary<long, int> ChuteIoAddressMap { get; set; } = new();

    /// <summary>
    /// 获取格口对应的IO状态地址
    /// </summary>
    /// <param name="chuteId">格口ID</param>
    /// <returns>IO状态地址，如果未找到返回null</returns>
    public int? GetIoAddress(long chuteId)
    {
        return ChuteIoAddressMap.TryGetValue(chuteId, out var address) ? address : null;
    }
}

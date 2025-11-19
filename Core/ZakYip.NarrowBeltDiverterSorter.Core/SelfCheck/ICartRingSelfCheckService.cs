using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Tracking;

namespace ZakYip.NarrowBeltDiverterSorter.Core.SelfCheck;

/// <summary>
/// 小车环自检服务接口
/// 基于小车通过事件分析小车环配置是否正确
/// </summary>
public interface ICartRingSelfCheckService
{
    /// <summary>
    /// 运行自检分析
    /// </summary>
    /// <param name="passEvents">小车通过事件列表</param>
    /// <param name="topologySnapshot">拓扑配置快照</param>
    /// <returns>自检结果</returns>
    CartRingSelfCheckResult RunAnalysis(
        IReadOnlyList<CartPassEventArgs> passEvents,
        TrackTopologySnapshot topologySnapshot);
}

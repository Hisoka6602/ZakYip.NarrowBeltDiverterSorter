namespace ZakYip.NarrowBeltDiverterSorter.Core.SelfCheck;

/// <summary>
/// 格口-小车映射自检服务。
/// </summary>
public interface IChuteCartMappingSelfCheckService
{
    /// <summary>
    /// 基于采集到的格口 IO 触发事件和拓扑信息，执行自检。
    /// </summary>
    ChuteCartMappingSelfCheckResult Analyze(
        IReadOnlyList<ChutePassEventArgs> chutePassEvents,
        TrackTopologySnapshot topology,
        ChuteCartMappingSelfCheckOptions options);
}

namespace ZakYip.NarrowBeltDiverterSorter.Simulation;

/// <summary>
/// 窄带分拣仿真报告服务接口。
/// </summary>
public interface INarrowBeltSimulationReportService
{
    /// <summary>
    /// 保存仿真报告。
    /// </summary>
    /// <param name="runId">运行ID</param>
    /// <param name="report">仿真报告</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task SaveReportAsync(string runId, SimulationReport report, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取仿真报告。
    /// </summary>
    /// <param name="runId">运行ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>仿真报告，如果不存在返回null</returns>
    Task<SimulationReport?> GetReportAsync(string runId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取所有仿真报告的运行ID列表。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>运行ID列表</returns>
    Task<IReadOnlyList<string>> GetAllRunIdsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除仿真报告。
    /// </summary>
    /// <param name="runId">运行ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeleteReportAsync(string runId, CancellationToken cancellationToken = default);
}

using ZakYip.NarrowBeltDiverterSorter.Core.Configuration;

namespace ZakYip.NarrowBeltDiverterSorter.Simulation;

/// <summary>
/// 窄带分拣仿真场景运行器接口。
/// 负责根据配置执行场景级仿真，并收集统计信息。
/// </summary>
public interface INarrowBeltSimulationScenarioRunner
{
    /// <summary>
    /// 执行仿真场景。
    /// </summary>
    /// <param name="simulationOptions">仿真配置选项</param>
    /// <param name="chuteLayout">格口布局配置</param>
    /// <param name="assignmentProfile">目标格口分配策略配置</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>仿真报告</returns>
    Task<SimulationReport> RunAsync(
        NarrowBeltSimulationOptions simulationOptions,
        ChuteLayoutProfile chuteLayout,
        TargetChuteAssignmentProfile assignmentProfile,
        CancellationToken cancellationToken = default);
}

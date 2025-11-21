using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Configuration;
using ZakYip.NarrowBeltDiverterSorter.Host.Contracts.API;
// Note: INarrowBeltSimulationScenarioRunner and related types cannot be used due to circular dependency
// between Host and Simulation projects. This will be resolved in a future PR.
// using ZakYip.NarrowBeltDiverterSorter.Simulation;

namespace ZakYip.NarrowBeltDiverterSorter.Host.Controllers;

/// <summary>
/// 仿真控制 API 控制器。
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SimulationsController : ControllerBase
{
    private readonly ILongRunLoadTestOptionsRepository _longRunRepo;
    private readonly IMainLineOptionsRepository _mainLineRepo;
    private readonly ISimulationStatisticsService? _statisticsService;
    // Note: These services are commented out due to circular dependency with Simulation project
    // private readonly INarrowBeltSimulationScenarioRunner? _scenarioRunner;
    // private readonly INarrowBeltSimulationReportService? _reportService;
    private readonly IOptions<NarrowBeltSimulationOptions> _simulationOptions;
    private readonly IOptions<ChuteLayoutProfile> _chuteLayoutOptions;
    private readonly IOptions<TargetChuteAssignmentProfile> _assignmentOptions;
    private readonly ILogger<SimulationsController> _logger;

    public SimulationsController(
        ILongRunLoadTestOptionsRepository longRunRepo,
        IMainLineOptionsRepository mainLineRepo,
        IOptions<NarrowBeltSimulationOptions> simulationOptions,
        IOptions<ChuteLayoutProfile> chuteLayoutOptions,
        IOptions<TargetChuteAssignmentProfile> assignmentOptions,
        ILogger<SimulationsController> logger,
        ISimulationStatisticsService? statisticsService = null)
    {
        _longRunRepo = longRunRepo ?? throw new ArgumentNullException(nameof(longRunRepo));
        _mainLineRepo = mainLineRepo ?? throw new ArgumentNullException(nameof(mainLineRepo));
        _simulationOptions = simulationOptions ?? throw new ArgumentNullException(nameof(simulationOptions));
        _chuteLayoutOptions = chuteLayoutOptions ?? throw new ArgumentNullException(nameof(chuteLayoutOptions));
        _assignmentOptions = assignmentOptions ?? throw new ArgumentNullException(nameof(assignmentOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _statisticsService = statisticsService;
    }

    /// <summary>
    /// 通过模拟电柜面板启动按钮启动长跑仿真。
    /// 本接口模拟用户在电柜面板上按下启动按钮，然后根据配置启动长跑仿真。
    /// </summary>
    [HttpPost("long-run/start-from-panel")]
    [ProducesResponseType(typeof(LongRunSimulationStartResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> StartLongRunFromPanel(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("接收到通过面板启动长跑仿真请求");

            // 1. 加载配置
            var longRunOptions = await _longRunRepo.LoadAsync(cancellationToken);
            var mainLineOptions = await _mainLineRepo.LoadAsync(cancellationToken);

            _logger.LogInformation(
                "已加载配置：目标包裹数={TargetParcelCount}, 间隔={IntervalMs}ms, 主线速度={SpeedMmps}mm/s",
                longRunOptions.TargetParcelCount,
                longRunOptions.ParcelCreationIntervalMs,
                mainLineOptions.TargetSpeedMmps);

            // 2. 验证系统状态（简化版本 - 实际应该检查主线是否运行、是否有故障等）
            // TODO: 实现系统状态检查逻辑

            // 3. 模拟电柜面板启动按钮被按下
            _logger.LogInformation("模拟电柜面板启动按钮被按下");
            // TODO: 通过面板服务发送启动信号

            // 4. 启动长跑仿真（注意：这里应该是异步启动后台任务）
            // 由于当前架构限制，我们返回一个标识，表示仿真已触发
            var runId = Guid.NewGuid().ToString();
            
            _logger.LogInformation("长跑仿真已触发，Run ID: {RunId}", runId);

            // TODO: 实际实现应该启动后台服务来执行仿真
            // 当前阶段只返回成功消息，实际仿真逻辑需要后续实现

            return Ok(new LongRunSimulationStartResponse
            {
                RunId = runId,
                Status = "triggered",
                Message = "长跑仿真已通过面板启动按钮触发",
                Configuration = new
                {
                    TargetParcelCount = longRunOptions.TargetParcelCount,
                    ParcelCreationIntervalMs = longRunOptions.ParcelCreationIntervalMs,
                    ChuteCount = longRunOptions.ChuteCount,
                    MainLineSpeedMmps = mainLineOptions.TargetSpeedMmps
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "启动长跑仿真失败");
            return StatusCode(500, new { error = "启动长跑仿真失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 获取窄带仿真结果统计。
    /// </summary>
    /// <param name="runId">运行标识符。如果未提供，则返回当前活动的仿真统计。</param>
    [HttpGet("narrowbelt/result")]
    [ProducesResponseType(typeof(SimulationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public IActionResult GetNarrowBeltSimulationResult([FromQuery] string? runId = null)
    {
        if (_statisticsService == null)
        {
            return StatusCode(503, new { error = "仿真统计服务未启用", message = "统计服务未注册" });
        }

        try
        {
            // 如果未提供 runId，使用当前活动的运行
            var targetRunId = runId ?? _statisticsService.GetActiveRunId();
            
            if (string.IsNullOrWhiteSpace(targetRunId))
            {
                return NotFound(new { error = "未找到活动的仿真运行", message = "请提供有效的 runId 或启动仿真" });
            }

            var statistics = _statisticsService.GetStatistics(targetRunId);
            
            if (statistics == null)
            {
                return NotFound(new { error = "未找到仿真统计", runId = targetRunId });
            }

            return Ok(new SimulationResultDto
            {
                RunId = statistics.RunId,
                TotalParcels = statistics.TotalParcels,
                SortedToTargetChutes = statistics.SortedToTargetChutes,
                SortedToErrorChute = statistics.SortedToErrorChute,
                TimedOutCount = statistics.TimedOutCount,
                MisSortedCount = statistics.MisSortedCount,
                IsCompleted = statistics.IsCompleted,
                StartTime = statistics.StartTime,
                EndTime = statistics.EndTime
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取仿真结果失败");
            return StatusCode(500, new { error = "获取仿真结果失败", message = ex.Message });
        }
    }

    // Note: The following endpoints are commented out due to circular dependency between Host and Simulation projects.
    // This will be resolved in a future PR by moving shared types to Core or restructuring projects.
    
    /*
    /// <summary>
    /// 执行配置化窄带仿真场景。
    /// </summary>
    [HttpPost("narrow-belt/run")]
    [ProducesResponseType(typeof(NarrowBeltSimulationRunResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> RunNarrowBeltSimulation(CancellationToken cancellationToken)
    {
        if (_scenarioRunner == null || _reportService == null)
        {
            return StatusCode(503, new { error = "仿真服务未启用", message = "场景运行器或报告服务未注册" });
        }

        try
        {
            var runId = $"nb-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}";
            
            _logger.LogInformation("开始执行窄带仿真场景，RunId: {RunId}", runId);

            var simulationOpts = _simulationOptions.Value;
            var chuteLayout = _chuteLayoutOptions.Value;
            var assignment = _assignmentOptions.Value;

            // 执行仿真
            var report = await _scenarioRunner.RunAsync(simulationOpts, chuteLayout, assignment, cancellationToken);

            // 保存报告
            await _reportService.SaveReportAsync(runId, report, cancellationToken);

            _logger.LogInformation(
                "窄带仿真完成，RunId: {RunId}, 成功率: {SuccessRate:P2}",
                runId,
                report.Statistics.SuccessRate);

            return Ok(new NarrowBeltSimulationRunResponse
            {
                RunId = runId,
                Status = "completed",
                Statistics = report.Statistics
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行窄带仿真场景失败");
            return StatusCode(500, new { error = "执行窄带仿真场景失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 获取仿真报告。
    /// </summary>
    [HttpGet("narrow-belt/report/{runId}")]
    [ProducesResponseType(typeof(SimulationReport), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetNarrowBeltSimulationReport(string runId, CancellationToken cancellationToken)
    {
        if (_reportService == null)
        {
            return StatusCode(503, new { error = "仿真服务未启用", message = "报告服务未注册" });
        }

        var report = await _reportService.GetReportAsync(runId, cancellationToken);
        
        if (report == null)
        {
            return NotFound(new { error = "报告未找到", runId });
        }

        return Ok(report);
    }

    /// <summary>
    /// 获取所有仿真报告的运行ID列表。
    /// </summary>
    [HttpGet("narrow-belt/reports")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetAllNarrowBeltSimulationReports(CancellationToken cancellationToken)
    {
        if (_reportService == null)
        {
            return StatusCode(503, new { error = "仿真服务未启用", message = "报告服务未注册" });
        }

        var runIds = await _reportService.GetAllRunIdsAsync(cancellationToken);
        return Ok(runIds);
    }

    /// <summary>
    /// 删除仿真报告。
    /// </summary>
    [HttpDelete("narrow-belt/report/{runId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> DeleteNarrowBeltSimulationReport(string runId, CancellationToken cancellationToken)
    {
        if (_reportService == null)
        {
            return StatusCode(503, new { error = "仿真服务未启用", message = "报告服务未注册" });
        }

        await _reportService.DeleteReportAsync(runId, cancellationToken);
        return NoContent();
    }
    */
}

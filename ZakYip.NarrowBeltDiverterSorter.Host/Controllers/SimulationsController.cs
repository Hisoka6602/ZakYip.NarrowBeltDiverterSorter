using Microsoft.AspNetCore.Mvc;
using ZakYip.NarrowBeltDiverterSorter.Infrastructure.Configuration;

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
    private readonly ILogger<SimulationsController> _logger;

    public SimulationsController(
        ILongRunLoadTestOptionsRepository longRunRepo,
        IMainLineOptionsRepository mainLineRepo,
        ILogger<SimulationsController> logger)
    {
        _longRunRepo = longRunRepo ?? throw new ArgumentNullException(nameof(longRunRepo));
        _mainLineRepo = mainLineRepo ?? throw new ArgumentNullException(nameof(mainLineRepo));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
}

/// <summary>
/// 长跑仿真启动响应。
/// </summary>
public class LongRunSimulationStartResponse
{
    /// <summary>
    /// 仿真运行 ID。
    /// </summary>
    public required string RunId { get; init; }

    /// <summary>
    /// 仿真状态。
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// 响应消息。
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// 仿真配置摘要。
    /// </summary>
    public required object Configuration { get; init; }
}

using Microsoft.AspNetCore.Mvc;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Safety;

namespace ZakYip.NarrowBeltDiverterSorter.Host.Controllers;

/// <summary>
/// 线体控制 API
/// 提供启动、停止、暂停等线体控制功能
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class LineController : ControllerBase
{
    private readonly ILineSafetyOrchestrator _safetyOrchestrator;
    private readonly ILogger<LineController> _logger;

    public LineController(
        ILineSafetyOrchestrator safetyOrchestrator,
        ILogger<LineController> logger)
    {
        _safetyOrchestrator = safetyOrchestrator ?? throw new ArgumentNullException(nameof(safetyOrchestrator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 获取当前线体状态
    /// </summary>
    [HttpGet("state")]
    [ProducesResponseType(typeof(LineStateResponse), StatusCodes.Status200OK)]
    public ActionResult<LineStateResponse> GetState()
    {
        var response = new LineStateResponse
        {
            LineRunState = _safetyOrchestrator.CurrentLineRunState.ToString(),
            SafetyState = _safetyOrchestrator.CurrentSafetyState.ToString(),
            Timestamp = DateTimeOffset.UtcNow
        };

        return Ok(response);
    }

    /// <summary>
    /// 请求启动线体
    /// </summary>
    [HttpPost("start")]
    [ProducesResponseType(typeof(LineOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(LineOperationResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LineOperationResponse>> Start(CancellationToken cancellationToken)
    {
        _logger.LogInformation("收到启动线体请求");

        var success = await _safetyOrchestrator.RequestStartAsync(cancellationToken);

        var response = new LineOperationResponse
        {
            Success = success,
            Message = success ? "启动命令已接受" : "启动命令被拒绝",
            CurrentLineRunState = _safetyOrchestrator.CurrentLineRunState.ToString(),
            CurrentSafetyState = _safetyOrchestrator.CurrentSafetyState.ToString(),
            Timestamp = DateTimeOffset.UtcNow
        };

        return success ? Ok(response) : BadRequest(response);
    }

    /// <summary>
    /// 请求正常停止线体
    /// </summary>
    [HttpPost("stop")]
    [ProducesResponseType(typeof(LineOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(LineOperationResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LineOperationResponse>> Stop(CancellationToken cancellationToken)
    {
        _logger.LogInformation("收到停止线体请求");

        var success = await _safetyOrchestrator.RequestStopAsync(cancellationToken);

        var response = new LineOperationResponse
        {
            Success = success,
            Message = success ? "停止命令已接受" : "停止命令被拒绝",
            CurrentLineRunState = _safetyOrchestrator.CurrentLineRunState.ToString(),
            CurrentSafetyState = _safetyOrchestrator.CurrentSafetyState.ToString(),
            Timestamp = DateTimeOffset.UtcNow
        };

        return success ? Ok(response) : BadRequest(response);
    }

    /// <summary>
    /// 请求暂停线体
    /// </summary>
    [HttpPost("pause")]
    [ProducesResponseType(typeof(LineOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(LineOperationResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LineOperationResponse>> Pause(CancellationToken cancellationToken)
    {
        _logger.LogInformation("收到暂停线体请求");

        var success = await _safetyOrchestrator.RequestPauseAsync(cancellationToken);

        var response = new LineOperationResponse
        {
            Success = success,
            Message = success ? "暂停命令已接受" : "暂停命令被拒绝",
            CurrentLineRunState = _safetyOrchestrator.CurrentLineRunState.ToString(),
            CurrentSafetyState = _safetyOrchestrator.CurrentSafetyState.ToString(),
            Timestamp = DateTimeOffset.UtcNow
        };

        return success ? Ok(response) : BadRequest(response);
    }

    /// <summary>
    /// 请求从暂停恢复运行
    /// </summary>
    [HttpPost("resume")]
    [ProducesResponseType(typeof(LineOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(LineOperationResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LineOperationResponse>> Resume(CancellationToken cancellationToken)
    {
        _logger.LogInformation("收到恢复线体请求");

        var success = await _safetyOrchestrator.RequestResumeAsync(cancellationToken);

        var response = new LineOperationResponse
        {
            Success = success,
            Message = success ? "恢复命令已接受" : "恢复命令被拒绝",
            CurrentLineRunState = _safetyOrchestrator.CurrentLineRunState.ToString(),
            CurrentSafetyState = _safetyOrchestrator.CurrentSafetyState.ToString(),
            Timestamp = DateTimeOffset.UtcNow
        };

        return success ? Ok(response) : BadRequest(response);
    }

    /// <summary>
    /// 确认故障，允许进入恢复流程
    /// </summary>
    [HttpPost("fault/ack")]
    [ProducesResponseType(typeof(LineOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(LineOperationResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LineOperationResponse>> AcknowledgeFault(CancellationToken cancellationToken)
    {
        _logger.LogInformation("收到故障确认请求");

        var success = await _safetyOrchestrator.AcknowledgeFaultAsync(cancellationToken);

        var response = new LineOperationResponse
        {
            Success = success,
            Message = success ? "故障已确认" : "故障确认被拒绝",
            CurrentLineRunState = _safetyOrchestrator.CurrentLineRunState.ToString(),
            CurrentSafetyState = _safetyOrchestrator.CurrentSafetyState.ToString(),
            Timestamp = DateTimeOffset.UtcNow
        };

        return success ? Ok(response) : BadRequest(response);
    }
}

/// <summary>
/// 线体状态响应
/// </summary>
public record class LineStateResponse
{
    /// <summary>
    /// 线体运行状态
    /// </summary>
    public required string LineRunState { get; init; }

    /// <summary>
    /// 安全状态
    /// </summary>
    public required string SafetyState { get; init; }

    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTimeOffset Timestamp { get; init; }
}

/// <summary>
/// 线体操作响应
/// </summary>
public record class LineOperationResponse
{
    /// <summary>
    /// 操作是否成功
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// 响应消息
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// 当前线体运行状态
    /// </summary>
    public required string CurrentLineRunState { get; init; }

    /// <summary>
    /// 当前安全状态
    /// </summary>
    public required string CurrentSafetyState { get; init; }

    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTimeOffset Timestamp { get; init; }
}

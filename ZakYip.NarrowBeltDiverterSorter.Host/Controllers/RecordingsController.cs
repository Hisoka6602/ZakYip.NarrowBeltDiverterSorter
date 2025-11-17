using Microsoft.AspNetCore.Mvc;
using ZakYip.NarrowBeltDiverterSorter.Host.DTOs.Recording;
using ZakYip.NarrowBeltDiverterSorter.Observability.Recording;

namespace ZakYip.NarrowBeltDiverterSorter.Host.Controllers;

/// <summary>
/// 录制与回放控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class RecordingsController : ControllerBase
{
    private readonly IEventRecordingManager _recordingManager;
    private readonly ILogger<RecordingsController> _logger;
    // TODO: 回放功能将在后续实现
    // private readonly IRecordingReplayRunner? _replayRunner;

    public RecordingsController(
        IEventRecordingManager recordingManager,
        ILogger<RecordingsController> logger)
    {
        _recordingManager = recordingManager ?? throw new ArgumentNullException(nameof(recordingManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 启动新的录制会话
    /// </summary>
    [HttpPost("start")]
    [ProducesResponseType(typeof(RecordingSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> StartRecording(
        [FromBody] StartRecordingRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting recording session '{Name}'", request.Name);

            var session = await _recordingManager.StartSessionAsync(
                request.Name,
                request.Description,
                cancellationToken);

            _logger.LogInformation("Recording session {SessionId} started successfully", session.SessionId);

            return Ok(MapToResponse(session));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to start recording session");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting recording session");
            return StatusCode(500, new { error = "Failed to start recording session", message = ex.Message });
        }
    }

    /// <summary>
    /// 停止指定的录制会话
    /// </summary>
    [HttpPost("{sessionId}/stop")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> StopRecording(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Stopping recording session {SessionId}", sessionId);

            await _recordingManager.StopSessionAsync(sessionId, cancellationToken);

            _logger.LogInformation("Recording session {SessionId} stopped successfully", sessionId);

            var session = await _recordingManager.GetSessionAsync(sessionId, cancellationToken);
            if (session == null)
            {
                return NotFound(new { error = "Session not found", sessionId });
            }

            return Ok(MapToResponse(session));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to stop recording session {SessionId}", sessionId);
            return BadRequest(new { error = ex.Message, sessionId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping recording session {SessionId}", sessionId);
            return StatusCode(500, new { error = "Failed to stop recording session", message = ex.Message, sessionId });
        }
    }

    /// <summary>
    /// 获取所有录制会话列表
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<RecordingSessionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ListRecordings(CancellationToken cancellationToken)
    {
        try
        {
            var sessions = await _recordingManager.ListSessionsAsync(cancellationToken);
            var response = sessions.Select(MapToResponse).ToList();

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing recording sessions");
            return StatusCode(500, new { error = "Failed to list recording sessions", message = ex.Message });
        }
    }

    /// <summary>
    /// 获取指定录制会话的详细信息
    /// </summary>
    [HttpGet("{sessionId}")]
    [ProducesResponseType(typeof(RecordingSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetRecording(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await _recordingManager.GetSessionAsync(sessionId, cancellationToken);

            if (session == null)
            {
                return NotFound(new { error = "Session not found", sessionId });
            }

            return Ok(MapToResponse(session));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recording session {SessionId}", sessionId);
            return StatusCode(500, new { error = "Failed to get recording session", message = ex.Message, sessionId });
        }
    }

    /// <summary>
    /// 回放指定的录制会话
    /// </summary>
    [HttpPost("{sessionId}/replay")]
    [ProducesResponseType(typeof(ReplayResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> ReplayRecording(
        Guid sessionId,
        [FromBody] ReplayRequest request,
        CancellationToken cancellationToken)
    {
        // TODO: 回放功能将在后续实现
        _logger.LogWarning("Replay functionality not yet implemented for session {SessionId}", sessionId);
        
        return StatusCode(501, new
        {
            error = "Replay functionality not yet implemented",
            message = "Replay runner will be implemented in a future phase",
            sessionId
        });
    }

    private static RecordingSessionResponse MapToResponse(RecordingSessionInfo session)
    {
        return new RecordingSessionResponse
        {
            SessionId = session.SessionId,
            Name = session.Name,
            StartedAt = session.StartedAt,
            StoppedAt = session.StoppedAt,
            Description = session.Description,
            IsCompleted = session.IsCompleted,
            EventCount = session.EventCount
        };
    }
}

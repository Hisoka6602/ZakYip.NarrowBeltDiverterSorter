using Microsoft.AspNetCore.Mvc;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Parcels;

namespace ZakYip.NarrowBeltDiverterSorter.Host.Controllers;

/// <summary>
/// 包裹时间线诊断 API 控制器
/// </summary>
[ApiController]
[Route("api/diagnostics/parcel-timeline")]
public class ParcelTimelineController : ControllerBase
{
    private readonly IParcelTimelineService _timelineService;
    private readonly ILogger<ParcelTimelineController> _logger;

    public ParcelTimelineController(
        IParcelTimelineService timelineService,
        ILogger<ParcelTimelineController> logger)
    {
        _timelineService = timelineService ?? throw new ArgumentNullException(nameof(timelineService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 获取指定包裹的时间线事件
    /// </summary>
    /// <param name="parcelId">包裹 ID</param>
    /// <param name="maxCount">最大返回数量（默认 100）</param>
    /// <returns>包裹时间线事件列表</returns>
    /// <response code="200">成功返回包裹时间线</response>
    /// <response code="404">包裹不存在或无时间线事件</response>
    [HttpGet("{parcelId}")]
    [ProducesResponseType(typeof(List<ParcelTimelineEventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetByParcelId(long parcelId, [FromQuery] int maxCount = 100)
    {
        try
        {
            var events = _timelineService.QueryByParcel(parcelId, maxCount);

            if (events.Count == 0)
            {
                return NotFound(new { error = "未找到包裹时间线", parcelId });
            }

            var dtos = events.Select(e => new ParcelTimelineEventDto
            {
                ParcelId = e.ParcelId,
                EventType = e.EventType.ToString(),
                EventTypeDescription = GetEventTypeDescription(e.EventType),
                OccurredAt = e.OccurredAt,
                Barcode = e.Barcode,
                ChuteId = e.ChuteId,
                CartId = e.CartId,
                UpstreamCorrelationId = e.UpstreamCorrelationId,
                Note = e.Note
            }).ToList();

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询包裹时间线失败: ParcelId={ParcelId}", parcelId);
            return StatusCode(500, new { error = "查询失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 获取最近的时间线事件
    /// </summary>
    /// <param name="count">返回数量（默认 100，最大 1000）</param>
    /// <returns>最近的时间线事件列表</returns>
    /// <response code="200">成功返回时间线事件</response>
    [HttpGet("recent")]
    [ProducesResponseType(typeof(List<ParcelTimelineEventDto>), StatusCodes.Status200OK)]
    public IActionResult GetRecent([FromQuery] int count = 100)
    {
        try
        {
            // 限制最大数量为 1000
            var maxCount = Math.Min(count, 1000);
            var events = _timelineService.QueryRecent(maxCount);

            var dtos = events.Select(e => new ParcelTimelineEventDto
            {
                ParcelId = e.ParcelId,
                EventType = e.EventType.ToString(),
                EventTypeDescription = GetEventTypeDescription(e.EventType),
                OccurredAt = e.OccurredAt,
                Barcode = e.Barcode,
                ChuteId = e.ChuteId,
                CartId = e.CartId,
                UpstreamCorrelationId = e.UpstreamCorrelationId,
                Note = e.Note
            }).ToList();

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询最近时间线事件失败");
            return StatusCode(500, new { error = "查询失败", message = ex.Message });
        }
    }

    private static string GetEventTypeDescription(ParcelTimelineEventType eventType)
    {
        return eventType switch
        {
            ParcelTimelineEventType.Created => "包裹创建",
            ParcelTimelineEventType.DwsAttached => "DWS数据附加",
            ParcelTimelineEventType.UpstreamRequestSent => "上游请求已发送",
            ParcelTimelineEventType.UpstreamResultReceived => "上游结果已接收",
            ParcelTimelineEventType.PlanCreated => "分拣计划已创建",
            ParcelTimelineEventType.LoadedToCart => "已装载到小车",
            ParcelTimelineEventType.ApproachingChute => "接近格口",
            ParcelTimelineEventType.DivertedToChute => "已落格",
            ParcelTimelineEventType.DivertFailed => "落格失败",
            ParcelTimelineEventType.Completed => "已完成",
            ParcelTimelineEventType.Aborted => "已中断",
            _ => eventType.ToString()
        };
    }
}

/// <summary>
/// 包裹时间线事件 DTO
/// </summary>
public record class ParcelTimelineEventDto
{
    /// <summary>
    /// 包裹 ID
    /// </summary>
    public long ParcelId { get; init; }

    /// <summary>
    /// 事件类型
    /// </summary>
    public string EventType { get; init; } = string.Empty;

    /// <summary>
    /// 事件类型描述
    /// </summary>
    public string EventTypeDescription { get; init; } = string.Empty;

    /// <summary>
    /// 发生时间
    /// </summary>
    public DateTimeOffset OccurredAt { get; init; }

    /// <summary>
    /// 条码
    /// </summary>
    public string? Barcode { get; init; }

    /// <summary>
    /// 格口 ID
    /// </summary>
    public long? ChuteId { get; init; }

    /// <summary>
    /// 小车 ID
    /// </summary>
    public long? CartId { get; init; }

    /// <summary>
    /// 上游关联 ID
    /// </summary>
    public string? UpstreamCorrelationId { get; init; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Note { get; init; }
}

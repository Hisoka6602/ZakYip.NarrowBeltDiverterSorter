using Microsoft.AspNetCore.Mvc;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Parcels;
using ZakYip.NarrowBeltDiverterSorter.Host.Contracts.API;

namespace ZakYip.NarrowBeltDiverterSorter.Host.Controllers;

/// <summary>
/// 包裹生命周期查询 API 控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ParcelsController : ControllerBase
{
    private readonly IParcelLifecycleService _parcelLifecycleService;
    private readonly IParcelLifecycleTracker _lifecycleTracker;
    private readonly ILogger<ParcelsController> _logger;

    public ParcelsController(
        IParcelLifecycleService parcelLifecycleService,
        IParcelLifecycleTracker lifecycleTracker,
        ILogger<ParcelsController> logger)
    {
        _parcelLifecycleService = parcelLifecycleService ?? throw new ArgumentNullException(nameof(parcelLifecycleService));
        _lifecycleTracker = lifecycleTracker ?? throw new ArgumentNullException(nameof(lifecycleTracker));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 获取指定包裹的生命周期信息
    /// </summary>
    /// <param name="parcelId">包裹ID</param>
    /// <returns>包裹生命周期信息</returns>
    [HttpGet("{parcelId}/lifecycle")]
    [ProducesResponseType(typeof(ParcelLifecycleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetParcelLifecycle(long parcelId)
    {
        try
        {
            var snapshot = _lifecycleTracker.GetParcelSnapshot(new ParcelId(parcelId));
            if (snapshot == null)
            {
                // 尝试从基础服务获取
                snapshot = _parcelLifecycleService.Get(new ParcelId(parcelId));
                if (snapshot == null)
                {
                    return NotFound(new { message = $"包裹 {parcelId} 不存在" });
                }
            }

            var dto = MapToDto(snapshot);
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取包裹生命周期失败：ParcelId={ParcelId}", parcelId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "获取包裹生命周期失败" });
        }
    }

    /// <summary>
    /// 获取当前在线包裹列表
    /// </summary>
    /// <returns>在线包裹列表</returns>
    [HttpGet("online")]
    [ProducesResponseType(typeof(IReadOnlyList<ParcelLifecycleDto>), StatusCodes.Status200OK)]
    public IActionResult GetOnlineParcels()
    {
        try
        {
            var parcels = _lifecycleTracker.GetOnlineParcels();
            var dtos = parcels.Select(MapToDto).ToList();
            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取在线包裹列表失败");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "获取在线包裹列表失败" });
        }
    }

    /// <summary>
    /// 获取最近完成的包裹列表
    /// </summary>
    /// <param name="count">返回数量，默认100</param>
    /// <returns>最近完成的包裹列表</returns>
    [HttpGet("recent-completed")]
    [ProducesResponseType(typeof(IReadOnlyList<ParcelLifecycleDto>), StatusCodes.Status200OK)]
    public IActionResult GetRecentCompletedParcels([FromQuery] int count = 100)
    {
        try
        {
            if (count <= 0 || count > 1000)
            {
                return BadRequest(new { message = "count 参数必须在 1-1000 之间" });
            }

            var parcels = _lifecycleTracker.GetRecentCompletedParcels(count);
            var dtos = parcels.Select(MapToDto).ToList();
            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取最近完成包裹列表失败");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "获取最近完成包裹列表失败" });
        }
    }

    /// <summary>
    /// 获取包裹生命周期统计信息
    /// </summary>
    /// <returns>统计信息</returns>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(ParcelLifecycleStatsDto), StatusCodes.Status200OK)]
    public IActionResult GetStats()
    {
        try
        {
            var statusDist = _lifecycleTracker.GetStatusDistribution();
            var failureDist = _lifecycleTracker.GetFailureReasonDistribution();

            var dto = new ParcelLifecycleStatsDto
            {
                StatusDistribution = statusDist.ToDictionary(
                    kvp => kvp.Key.ToString(),
                    kvp => kvp.Value),
                FailureReasonDistribution = failureDist.ToDictionary(
                    kvp => kvp.Key.ToString(),
                    kvp => kvp.Value),
                OnlineCount = _lifecycleTracker.GetOnlineParcels().Count,
                TotalTracked = statusDist.Values.Sum()
            };

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取包裹统计信息失败");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "获取包裹统计信息失败" });
        }
    }

    private static ParcelLifecycleDto MapToDto(ParcelSnapshot snapshot)
    {
        return new ParcelLifecycleDto
        {
            ParcelId = snapshot.ParcelId.Value,
            Status = snapshot.Status.ToString(),
            FailureReason = snapshot.FailureReason.ToString(),
            RouteState = snapshot.RouteState.ToString(),
            TargetChuteId = snapshot.TargetChuteId?.Value,
            ActualChuteId = snapshot.ActualChuteId?.Value,
            BoundCartId = snapshot.BoundCartId?.Value,
            PredictedCartId = snapshot.PredictedCartId?.Value,
            CreatedAt = snapshot.CreatedAt,
            LoadedAt = snapshot.LoadedAt,
            DivertPlannedAt = snapshot.DivertPlannedAt,
            DivertedAt = snapshot.DivertedAt,
            SortedAt = snapshot.SortedAt,
            CompletedAt = snapshot.CompletedAt,
            SortingOutcome = snapshot.SortingOutcome?.ToString(),
            DiscardReason = snapshot.DiscardReason?.ToString()
        };
    }
}

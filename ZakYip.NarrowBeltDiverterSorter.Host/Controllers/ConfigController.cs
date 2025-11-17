using Microsoft.AspNetCore.Mvc;
using ZakYip.NarrowBeltDiverterSorter.Communication.Upstream;
using ZakYip.NarrowBeltDiverterSorter.Core.Configuration;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Feeding;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.MainLine;
using ZakYip.NarrowBeltDiverterSorter.Host.DTOs;
using ZakYip.NarrowBeltDiverterSorter.Infrastructure.Configuration;

namespace ZakYip.NarrowBeltDiverterSorter.Host.Controllers;

/// <summary>
/// 配置管理 API 控制器。
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ConfigController : ControllerBase
{
    private readonly IMainLineOptionsRepository _mainLineRepo;
    private readonly IInfeedLayoutOptionsRepository _infeedLayoutRepo;
    private readonly IUpstreamConnectionOptionsRepository _upstreamConnectionRepo;
    private readonly ILongRunLoadTestOptionsRepository _longRunLoadTestRepo;
    private readonly ILogger<ConfigController> _logger;

    public ConfigController(
        IMainLineOptionsRepository mainLineRepo,
        IInfeedLayoutOptionsRepository infeedLayoutRepo,
        IUpstreamConnectionOptionsRepository upstreamConnectionRepo,
        ILongRunLoadTestOptionsRepository longRunLoadTestRepo,
        ILogger<ConfigController> logger)
    {
        _mainLineRepo = mainLineRepo ?? throw new ArgumentNullException(nameof(mainLineRepo));
        _infeedLayoutRepo = infeedLayoutRepo ?? throw new ArgumentNullException(nameof(infeedLayoutRepo));
        _upstreamConnectionRepo = upstreamConnectionRepo ?? throw new ArgumentNullException(nameof(upstreamConnectionRepo));
        _longRunLoadTestRepo = longRunLoadTestRepo ?? throw new ArgumentNullException(nameof(longRunLoadTestRepo));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 获取主线控制选项。
    /// </summary>
    [HttpGet("mainline")]
    [ProducesResponseType(typeof(MainLineControlOptionsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMainLineOptions(CancellationToken cancellationToken)
    {
        try
        {
            var options = await _mainLineRepo.LoadAsync(cancellationToken);
            var dto = new MainLineControlOptionsDto
            {
                MaxSpeedMmps = options.MaxSpeedMmps,
                SteadySpeedMmps = options.SteadySpeedMmps,
                CartWidthMm = options.CartWidthMm,
                CartSpacingMm = options.CartSpacingMm,
                CartCount = options.CartCount,
                ProportionalGain = options.ProportionalGain,
                IntegralGain = options.IntegralGain,
                DerivativeGain = options.DerivativeGain
            };
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取主线控制选项失败");
            return StatusCode(500, new { error = "获取主线控制选项失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 更新主线控制选项。
    /// </summary>
    [HttpPut("mainline")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateMainLineOptions(
        [FromBody] MainLineControlOptionsDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            // 基础验证
            if (dto.CartCount <= 0)
                return BadRequest(new { error = "小车数量必须大于 0" });

            if (dto.SteadySpeedMmps <= 0)
                return BadRequest(new { error = "主线速度必须大于 0" });

            if (dto.CartWidthMm <= 0)
                return BadRequest(new { error = "小车宽度必须大于 0" });

            if (dto.CartSpacingMm <= 0)
                return BadRequest(new { error = "小车节距必须大于 0" });

            var options = new MainLineControlOptions
            {
                MaxSpeedMmps = dto.MaxSpeedMmps,
                SteadySpeedMmps = dto.SteadySpeedMmps,
                CartWidthMm = dto.CartWidthMm,
                CartSpacingMm = dto.CartSpacingMm,
                CartCount = dto.CartCount,
                ProportionalGain = dto.ProportionalGain,
                IntegralGain = dto.IntegralGain,
                DerivativeGain = dto.DerivativeGain
            };

            await _mainLineRepo.SaveAsync(options, cancellationToken);
            _logger.LogInformation("主线控制选项已更新");
            return Ok(new { message = "主线控制选项已更新" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新主线控制选项失败");
            return StatusCode(500, new { error = "更新主线控制选项失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 获取入口布局选项。
    /// </summary>
    [HttpGet("infeed-layout")]
    [ProducesResponseType(typeof(InfeedLayoutOptionsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInfeedLayoutOptions(CancellationToken cancellationToken)
    {
        try
        {
            var options = await _infeedLayoutRepo.LoadAsync(cancellationToken);
            var dto = new InfeedLayoutOptionsDto
            {
                InfeedToDropDistanceMm = options.InfeedToDropDistanceMm,
                InfeedConveyorSpeedMmps = options.InfeedConveyorSpeedMmps
            };
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取入口布局选项失败");
            return StatusCode(500, new { error = "获取入口布局选项失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 更新入口布局选项。
    /// </summary>
    [HttpPut("infeed-layout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateInfeedLayoutOptions(
        [FromBody] InfeedLayoutOptionsDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            if (dto.InfeedToDropDistanceMm <= 0)
                return BadRequest(new { error = "入口到落车点距离必须大于 0" });

            if (dto.InfeedConveyorSpeedMmps <= 0)
                return BadRequest(new { error = "入口输送线速度必须大于 0" });

            var options = new InfeedLayoutOptions
            {
                InfeedToDropDistanceMm = dto.InfeedToDropDistanceMm,
                InfeedConveyorSpeedMmps = dto.InfeedConveyorSpeedMmps
            };

            await _infeedLayoutRepo.SaveAsync(options, cancellationToken);
            _logger.LogInformation("入口布局选项已更新");
            return Ok(new { message = "入口布局选项已更新" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新入口布局选项失败");
            return StatusCode(500, new { error = "更新入口布局选项失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 获取上游连接选项。
    /// </summary>
    [HttpGet("upstream-connection")]
    [ProducesResponseType(typeof(UpstreamConnectionOptionsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUpstreamConnectionOptions(CancellationToken cancellationToken)
    {
        try
        {
            var options = await _upstreamConnectionRepo.LoadAsync(cancellationToken);
            var dto = new UpstreamConnectionOptionsDto
            {
                BaseUrl = options.BaseUrl,
                RequestTimeoutSeconds = options.RequestTimeoutSeconds,
                AuthToken = options.AuthToken
            };
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取上游连接选项失败");
            return StatusCode(500, new { error = "获取上游连接选项失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 更新上游连接选项。
    /// </summary>
    [HttpPut("upstream-connection")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateUpstreamConnectionOptions(
        [FromBody] UpstreamConnectionOptionsDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.BaseUrl))
                return BadRequest(new { error = "BaseUrl 不能为空" });

            if (dto.RequestTimeoutSeconds <= 0)
                return BadRequest(new { error = "请求超时时间必须大于 0" });

            var options = new UpstreamConnectionOptions
            {
                BaseUrl = dto.BaseUrl,
                RequestTimeoutSeconds = dto.RequestTimeoutSeconds,
                AuthToken = dto.AuthToken
            };

            await _upstreamConnectionRepo.SaveAsync(options, cancellationToken);
            _logger.LogInformation("上游连接选项已更新");
            return Ok(new { message = "上游连接选项已更新" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新上游连接选项失败");
            return StatusCode(500, new { error = "更新上游连接选项失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 获取长跑高负载测试选项。
    /// </summary>
    [HttpGet("long-run-load-test")]
    [ProducesResponseType(typeof(LongRunLoadTestOptionsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLongRunLoadTestOptions(CancellationToken cancellationToken)
    {
        try
        {
            var options = await _longRunLoadTestRepo.LoadAsync(cancellationToken);
            var dto = new LongRunLoadTestOptionsDto
            {
                TargetParcelCount = options.TargetParcelCount,
                ParcelCreationIntervalMs = options.ParcelCreationIntervalMs,
                ChuteCount = options.ChuteCount,
                ChuteWidthMm = options.ChuteWidthMm,
                MainLineSpeedMmps = options.MainLineSpeedMmps,
                CartWidthMm = options.CartWidthMm,
                CartSpacingMm = options.CartSpacingMm,
                CartCount = options.CartCount,
                ExceptionChuteId = options.ExceptionChuteId,
                MinParcelLengthMm = options.MinParcelLengthMm,
                MaxParcelLengthMm = options.MaxParcelLengthMm,
                ForceToExceptionChuteOnConflict = options.ForceToExceptionChuteOnConflict,
                InfeedToDropDistanceMm = options.InfeedToDropDistanceMm,
                InfeedConveyorSpeedMmps = options.InfeedConveyorSpeedMmps
            };
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取长跑测试选项失败");
            return StatusCode(500, new { error = "获取长跑测试选项失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 更新长跑高负载测试选项。
    /// </summary>
    [HttpPut("long-run-load-test")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateLongRunLoadTestOptions(
        [FromBody] LongRunLoadTestOptionsDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            // 基础验证
            if (dto.TargetParcelCount <= 0)
                return BadRequest(new { error = "目标包裹数必须大于 0" });

            if (dto.ParcelCreationIntervalMs <= 0)
                return BadRequest(new { error = "包裹创建间隔必须大于 0" });

            if (dto.ChuteCount <= 0)
                return BadRequest(new { error = "格口数量必须大于 0" });

            if (dto.ChuteWidthMm <= 0)
                return BadRequest(new { error = "格口宽度必须大于 0" });

            if (dto.MainLineSpeedMmps <= 0)
                return BadRequest(new { error = "主线速度必须大于 0" });

            if (dto.CartWidthMm <= 0)
                return BadRequest(new { error = "小车宽度必须大于 0" });

            if (dto.CartSpacingMm <= 0)
                return BadRequest(new { error = "小车节距必须大于 0" });

            if (dto.CartCount <= 0)
                return BadRequest(new { error = "小车数量必须大于 0" });

            if (dto.MinParcelLengthMm <= 0 || dto.MaxParcelLengthMm <= 0)
                return BadRequest(new { error = "包裹长度必须大于 0" });

            if (dto.MinParcelLengthMm > dto.MaxParcelLengthMm)
                return BadRequest(new { error = "最小包裹长度不能大于最大包裹长度" });

            var options = new LongRunLoadTestOptions
            {
                TargetParcelCount = dto.TargetParcelCount,
                ParcelCreationIntervalMs = dto.ParcelCreationIntervalMs,
                ChuteCount = dto.ChuteCount,
                ChuteWidthMm = dto.ChuteWidthMm,
                MainLineSpeedMmps = dto.MainLineSpeedMmps,
                CartWidthMm = dto.CartWidthMm,
                CartSpacingMm = dto.CartSpacingMm,
                CartCount = dto.CartCount,
                ExceptionChuteId = dto.ExceptionChuteId,
                MinParcelLengthMm = dto.MinParcelLengthMm,
                MaxParcelLengthMm = dto.MaxParcelLengthMm,
                ForceToExceptionChuteOnConflict = dto.ForceToExceptionChuteOnConflict,
                InfeedToDropDistanceMm = dto.InfeedToDropDistanceMm,
                InfeedConveyorSpeedMmps = dto.InfeedConveyorSpeedMmps
            };

            await _longRunLoadTestRepo.SaveAsync(options, cancellationToken);
            _logger.LogInformation("长跑测试选项已更新");
            return Ok(new { message = "长跑测试选项已更新" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新长跑测试选项失败");
            return StatusCode(500, new { error = "更新长跑测试选项失败", message = ex.Message });
        }
    }
}

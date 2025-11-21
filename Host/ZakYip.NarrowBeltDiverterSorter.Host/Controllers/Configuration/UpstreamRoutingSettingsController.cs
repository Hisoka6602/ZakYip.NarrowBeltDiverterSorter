using Microsoft.AspNetCore.Mvc;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Configuration;
using ZakYip.NarrowBeltDiverterSorter.Host.Contracts.Configuration;

namespace ZakYip.NarrowBeltDiverterSorter.Host.Controllers.Configuration;

/// <summary>
/// 上游路由配置管理控制器
/// </summary>
/// <remarks>
/// 提供查询和更新上游路由配置（TTL、异常格口等）的API接口
/// </remarks>
[ApiController]
[Route("api/settings/upstream-routing")]
public class UpstreamRoutingSettingsController : ControllerBase
{
    private readonly IUpstreamRoutingConfigProvider _configProvider;
    private readonly ILogger<UpstreamRoutingSettingsController> _logger;

    /// <summary>
    /// 初始化上游路由配置控制器
    /// </summary>
    /// <param name="configProvider">配置提供器</param>
    /// <param name="logger">日志记录器</param>
    public UpstreamRoutingSettingsController(
        IUpstreamRoutingConfigProvider configProvider,
        ILogger<UpstreamRoutingSettingsController> logger)
    {
        _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 获取当前的上游路由配置
    /// </summary>
    /// <remarks>
    /// 返回当前系统使用的上游路由配置，包括结果超时时间（TTL）和异常格口ID
    /// </remarks>
    /// <returns>上游路由配置</returns>
    /// <response code="200">成功返回配置</response>
    /// <response code="500">服务器内部错误</response>
    [HttpGet]
    [ProducesResponseType(typeof(UpstreamRoutingSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult GetSettings()
    {
        try
        {
            var options = _configProvider.GetCurrentOptions();
            
            var dto = new UpstreamRoutingSettingsDto
            {
                UpstreamResultTtlSeconds = (int)options.UpstreamResultTtl.TotalSeconds,
                ErrorChuteId = options.ErrorChuteId
            };

            _logger.LogDebug(
                "返回上游路由配置：TTL={TtlSeconds}秒，异常格口={ErrorChuteId}",
                dto.UpstreamResultTtlSeconds,
                dto.ErrorChuteId);

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取上游路由配置失败");
            return StatusCode(500, new { error = "获取上游路由配置失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 更新上游路由配置
    /// </summary>
    /// <remarks>
    /// 更新上游路由配置，配置将立即生效并持久化到数据库。
    /// 新配置将影响后续创建的包裹，已创建的包裹仍使用旧配置。
    /// 
    /// 示例请求：
    /// 
    ///     PUT /api/settings/upstream-routing
    ///     {
    ///       "upstreamResultTtlSeconds": 45,
    ///       "errorChuteId": 9999
    ///     }
    /// 
    /// </remarks>
    /// <param name="dto">新的配置</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    /// <response code="200">配置更新成功</response>
    /// <response code="400">请求参数验证失败</response>
    /// <response code="500">服务器内部错误</response>
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateSettings(
        [FromBody] UpstreamRoutingSettingsDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            // 验证输入
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // 转换为领域模型
            var newOptions = new UpstreamRoutingOptions
            {
                UpstreamResultTtl = TimeSpan.FromSeconds(dto.UpstreamResultTtlSeconds),
                ErrorChuteId = dto.ErrorChuteId
            };

            // 更新配置（这将同时更新内存和持久化到数据库）
            await _configProvider.UpdateOptionsAsync(newOptions, cancellationToken);

            _logger.LogInformation(
                "上游路由配置已更新：TTL={TtlSeconds}秒，异常格口={ErrorChuteId}",
                dto.UpstreamResultTtlSeconds,
                dto.ErrorChuteId);

            return Ok(new 
            { 
                message = "上游路由配置已更新",
                upstreamResultTtlSeconds = dto.UpstreamResultTtlSeconds,
                errorChuteId = dto.ErrorChuteId
            });
        }
        catch (Core.Configuration.ConfigurationAccessException ex)
        {
            _logger.LogError(ex, "更新上游路由配置失败（配置访问异常）");
            return StatusCode(500, new { error = "更新配置失败", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新上游路由配置失败");
            return StatusCode(500, new { error = "更新配置失败", message = ex.Message });
        }
    }
}

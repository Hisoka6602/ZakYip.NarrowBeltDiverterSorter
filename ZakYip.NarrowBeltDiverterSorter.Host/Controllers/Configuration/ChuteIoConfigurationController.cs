using Microsoft.AspNetCore.Mvc;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Chutes;
using ZakYip.NarrowBeltDiverterSorter.Host.Contracts.Configuration.Chutes;
using ZakYip.NarrowBeltDiverterSorter.Infrastructure.LiteDb;

namespace ZakYip.NarrowBeltDiverterSorter.Host.Controllers.Configuration;

/// <summary>
/// 格口 IO 配置管理控制器。
/// </summary>
[ApiController]
[Route("api/config/chutes")]
public class ChuteIoConfigurationController : ControllerBase
{
    private readonly LiteDbSorterConfigurationStore _configStore;
    private readonly ILogger<ChuteIoConfigurationController> _logger;

    public ChuteIoConfigurationController(
        LiteDbSorterConfigurationStore configStore,
        ILogger<ChuteIoConfigurationController> logger)
    {
        _configStore = configStore ?? throw new ArgumentNullException(nameof(configStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 获取全部格口 IO 配置列表。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>格口 IO 配置列表</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<ChuteIoConfigDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllAsync(CancellationToken cancellationToken)
    {
        try
        {
            var bindings = await _configStore.GetAllBindingsAsync(cancellationToken);
            var dtos = bindings.Select(b => new ChuteIoConfigDto
            {
                ChuteId = b.ChuteId,
                BusKey = b.BusKey,
                OutputBitIndex = b.OutputBitIndex,
                IsNormallyOn = b.IsNormallyOn
            }).ToList();

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取格口 IO 配置列表失败");
            return StatusCode(500, new { error = "获取格口 IO 配置列表失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 获取指定格口的 IO 配置。
    /// </summary>
    /// <param name="chuteId">格口Id</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>格口 IO 配置</returns>
    [HttpGet("{chuteId:long}")]
    [ProducesResponseType(typeof(ChuteIoConfigDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAsync(long chuteId, CancellationToken cancellationToken)
    {
        try
        {
            var bindings = await _configStore.GetAllBindingsAsync(cancellationToken);
            var binding = bindings.FirstOrDefault(b => b.ChuteId == chuteId);

            if (binding == null)
            {
                return NotFound(new { error = $"格口 {chuteId} 的 IO 配置不存在" });
            }

            var dto = new ChuteIoConfigDto
            {
                ChuteId = binding.ChuteId,
                BusKey = binding.BusKey,
                OutputBitIndex = binding.OutputBitIndex,
                IsNormallyOn = binding.IsNormallyOn
            };

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取格口 {ChuteId} 的 IO 配置失败", chuteId);
            return StatusCode(500, new { error = $"获取格口 {chuteId} 的 IO 配置失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 创建或更新指定格口的 IO 配置。
    /// </summary>
    /// <param name="chuteId">格口Id</param>
    /// <param name="dto">格口 IO 配置</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    [HttpPut("{chuteId:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpsertAsync(
        long chuteId,
        [FromBody] ChuteIoConfigDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            // 验证
            if (dto.ChuteId != chuteId)
            {
                return BadRequest(new { error = "URL 中的格口Id与请求体中的格口Id不一致" });
            }

            if (string.IsNullOrWhiteSpace(dto.BusKey))
            {
                return BadRequest(new { error = "总线标识不能为空" });
            }

            if (dto.OutputBitIndex < 0 || dto.OutputBitIndex > 31)
            {
                return BadRequest(new { error = "输出位索引必须在 0..31 范围内" });
            }

            var binding = new ChuteTransmitterBinding
            {
                ChuteId = dto.ChuteId,
                BusKey = dto.BusKey,
                OutputBitIndex = dto.OutputBitIndex,
                IsNormallyOn = dto.IsNormallyOn
            };

            await _configStore.UpsertBindingAsync(binding, cancellationToken);
            _logger.LogInformation("已保存格口 {ChuteId} 的 IO 配置", chuteId);

            return Ok(new { message = $"已保存格口 {chuteId} 的 IO 配置" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存格口 {ChuteId} 的 IO 配置失败", chuteId);
            return StatusCode(500, new { error = $"保存格口 {chuteId} 的 IO 配置失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 删除指定格口的 IO 配置。
    /// </summary>
    /// <param name="chuteId">格口Id</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    [HttpDelete("{chuteId:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteAsync(long chuteId, CancellationToken cancellationToken)
    {
        try
        {
            await _configStore.DeleteBindingAsync(chuteId, cancellationToken);
            _logger.LogInformation("已删除格口 {ChuteId} 的 IO 配置", chuteId);

            return Ok(new { message = $"已删除格口 {chuteId} 的 IO 配置" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除格口 {ChuteId} 的 IO 配置失败", chuteId);
            return StatusCode(500, new { error = $"删除格口 {chuteId} 的 IO 配置失败", message = ex.Message });
        }
    }
}

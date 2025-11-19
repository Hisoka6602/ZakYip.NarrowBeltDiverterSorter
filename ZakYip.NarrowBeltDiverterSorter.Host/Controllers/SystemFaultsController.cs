using Microsoft.AspNetCore.Mvc;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.SystemState;
using ZakYip.NarrowBeltDiverterSorter.Host.Contracts;

namespace ZakYip.NarrowBeltDiverterSorter.Host.Controllers;

/// <summary>
/// 系统故障管理控制器
/// 提供故障查询和复位功能
/// </summary>
[ApiController]
[Route("api/system/faults")]
public class SystemFaultsController : ControllerBase
{
    private readonly ISystemFaultService _faultService;
    private readonly ISystemRunStateService _runStateService;
    private readonly ILogger<SystemFaultsController> _logger;

    public SystemFaultsController(
        ISystemFaultService faultService,
        ISystemRunStateService runStateService,
        ILogger<SystemFaultsController> logger)
    {
        _faultService = faultService ?? throw new ArgumentNullException(nameof(faultService));
        _runStateService = runStateService ?? throw new ArgumentNullException(nameof(runStateService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 获取当前所有活动故障
    /// </summary>
    /// <returns>故障列表</returns>
    /// <response code="200">返回当前故障列表</response>
    [HttpGet]
    [ProducesResponseType(typeof(GetSystemFaultsResponse), StatusCodes.Status200OK)]
    public ActionResult<GetSystemFaultsResponse> GetFaults()
    {
        var faults = _faultService.GetActiveFaults();
        var hasBlockingFault = _faultService.HasBlockingFault();

        var response = new GetSystemFaultsResponse
        {
            Faults = faults.Select(f => new SystemFaultDto
            {
                FaultCode = f.FaultCode.ToString(),
                Message = f.Message,
                OccurredAt = f.OccurredAt,
                IsBlocking = f.IsBlocking
            }).ToList(),
            HasBlockingFault = hasBlockingFault,
            CurrentSystemState = _runStateService.Current.ToString()
        };

        return Ok(response);
    }

    /// <summary>
    /// 复位所有故障
    /// 清除故障标记并将系统状态从 Fault 切换到 Stopped
    /// 注意：此操作不会自动启动系统，需要手动按启动按钮或调用启动API
    /// </summary>
    /// <returns>操作结果</returns>
    /// <response code="200">故障复位成功</response>
    /// <response code="400">系统不在故障状态，无需复位</response>
    [HttpPost("reset")]
    [ProducesResponseType(typeof(ResetFaultsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(FaultErrorResponse), StatusCodes.Status400BadRequest)]
    public ActionResult<ResetFaultsResponse> ResetFaults()
    {
        // 检查系统状态
        if (_runStateService.Current != Core.Domain.SystemRunState.Fault)
        {
            _logger.LogWarning("尝试复位故障，但系统不在故障状态。当前状态: {State}", _runStateService.Current);
            return BadRequest(new FaultErrorResponse
            {
                Error = "系统当前不在故障状态，无需复位",
                CurrentState = _runStateService.Current.ToString()
            });
        }

        // 清除所有故障
        var faultsBefore = _faultService.GetActiveFaults().Count;
        _faultService.ClearAllFaults();

        // 通过急停解除方法将状态从 Fault 切换到 Stopped
        var resetResult = _runStateService.TryHandleEmergencyReset();
        if (!resetResult.IsSuccess)
        {
            _logger.LogError("故障复位失败: {Message}", resetResult.ErrorMessage);
            return BadRequest(new FaultErrorResponse
            {
                Error = resetResult.ErrorMessage,
                CurrentState = _runStateService.Current.ToString()
            });
        }

        _logger.LogInformation("故障已复位，清除了 {Count} 个故障，系统状态从 Fault 切换到 Stopped", faultsBefore);

        return Ok(new ResetFaultsResponse
        {
            Success = true,
            Message = $"已清除 {faultsBefore} 个故障，系统状态已切换到 Stopped。需要手动启动系统。",
            ClearedFaultCount = faultsBefore,
            NewSystemState = _runStateService.Current.ToString()
        });
    }
}

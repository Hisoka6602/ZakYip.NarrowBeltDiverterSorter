using Microsoft.AspNetCore.Mvc;
using ZakYip.NarrowBeltDiverterSorter.Communication.Upstream;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Sorting;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Upstream;
using ZakYip.NarrowBeltDiverterSorter.Observability.LiveView;

namespace ZakYip.NarrowBeltDiverterSorter.Host.Controllers;

/// <summary>
/// 上游诊断 API 控制器
/// </summary>
[ApiController]
[Route("api/upstream")]
public class UpstreamDiagnosticsController : ControllerBase
{
    private readonly INarrowBeltLiveView _liveView;
    private readonly ISortingRuleEnginePort _ruleEnginePort;
    private readonly ZakYip.NarrowBeltDiverterSorter.Host.Configuration.IHostConfigurationProvider _configProvider;
    private readonly ILogger<UpstreamDiagnosticsController> _logger;

    public UpstreamDiagnosticsController(
        INarrowBeltLiveView liveView,
        ISortingRuleEnginePort ruleEnginePort,
        ZakYip.NarrowBeltDiverterSorter.Host.Configuration.IHostConfigurationProvider configProvider,
        ILogger<UpstreamDiagnosticsController> logger)
    {
        _liveView = liveView ?? throw new ArgumentNullException(nameof(liveView));
        _ruleEnginePort = ruleEnginePort ?? throw new ArgumentNullException(nameof(ruleEnginePort));
        _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 获取上游规则引擎连接状态和统计信息
    /// </summary>
    /// <remarks>
    /// 返回当前上游配置模式、连接状态、请求统计、最后错误信息等。
    /// 
    /// 示例响应：
    /// <code>
    /// {
    ///   "mode": "Mqtt",
    ///   "status": "Connected",
    ///   "connectionAddress": "localhost:1883",
    ///   "totalRequests": 150,
    ///   "successfulResponses": 145,
    ///   "failedResponses": 5,
    ///   "averageLatencyMs": 25.3,
    ///   "lastError": null,
    ///   "lastErrorAt": null,
    ///   "lastUpdatedAt": "2025-11-19T05:30:00Z"
    /// }
    /// </code>
    /// </remarks>
    /// <returns>上游状态快照</returns>
    /// <response code="200">成功返回上游状态</response>
    [HttpGet("status")]
    [ProducesResponseType(typeof(UpstreamRuleEngineSnapshot), StatusCodes.Status200OK)]
    public IActionResult GetStatus()
    {
        try
        {
            var snapshot = _liveView.GetUpstreamRuleEngineStatus();
            return Ok(snapshot);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取上游状态失败");
            return StatusCode(500, new { error = "获取上游状态失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 发送测试包裹到上游规则引擎
    /// </summary>
    /// <remarks>
    /// 手动发送一个测试包裹请求到上游规则引擎，用于诊断和测试连接。
    /// 
    /// 示例请求：
    /// <code>
    /// {
    ///   "barcode": "TEST-12345",
    ///   "parcelId": "TEST-001"
    /// }
    /// </code>
    /// 
    /// 示例响应：
    /// <code>
    /// {
    ///   "success": true,
    ///   "message": "测试包裹发送成功",
    ///   "parcelId": "TEST-001",
    ///   "barcode": "TEST-12345",
    ///   "sentAt": "2025-11-19T05:30:00Z"
    /// }
    /// </code>
    /// </remarks>
    /// <param name="request">测试请求参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>测试结果</returns>
    /// <response code="200">测试包裹发送成功</response>
    /// <response code="400">请求参数无效</response>
    /// <response code="503">上游服务不可用或已禁用</response>
    [HttpPost("test-parcel")]
    [ProducesResponseType(typeof(TestParcelResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> TestParcel(
        [FromBody] TestParcelRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // 验证请求
            if (string.IsNullOrWhiteSpace(request.Barcode))
            {
                return BadRequest(new ErrorResponse
                {
                    Error = "参数验证失败",
                    Message = "条码 (Barcode) 不能为空"
                });
            }

            // 检查上游配置
            var upstreamOptions = await _configProvider.GetUpstreamOptionsAsync();
            if (upstreamOptions.Mode == UpstreamMode.Disabled)
            {
                return StatusCode(503, new ErrorResponse
                {
                    Error = "上游服务未启用",
                    Message = "当前上游模式为 Disabled，无法发送测试包裹。请在配置中启用 MQTT 或 TCP 模式。"
                });
            }

            // 检查连接状态
            var snapshot = _liveView.GetUpstreamRuleEngineStatus();
            if (snapshot.Status != UpstreamConnectionStatus.Connected)
            {
                return StatusCode(503, new ErrorResponse
                {
                    Error = "上游服务未连接",
                    Message = $"当前上游连接状态: {snapshot.Status}。请确保上游服务（RuleEngine）正在运行并可访问。"
                });
            }

            // 构造测试请求
            var parcelId = string.IsNullOrWhiteSpace(request.ParcelId) 
                ? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                : long.Parse(request.ParcelId);

            var testRequest = new SortingRequestEventArgs
            {
                ParcelId = parcelId,
                CartNumber = 999, // 测试用小车号
                Barcode = request.Barcode,
                Weight = 1.0m,
                Length = 300m,
                Width = 200m,
                Height = 150m,
                RequestTime = DateTimeOffset.UtcNow
            };

            _logger.LogInformation("发送测试包裹: ParcelId={ParcelId}, Barcode={Barcode}", parcelId, request.Barcode);

            // 发送请求（带超时）
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(10)); // 10秒超时

            try
            {
                await _ruleEnginePort.RequestSortingAsync(testRequest, cts.Token);

                return Ok(new TestParcelResponse
                {
                    Success = true,
                    Message = "测试包裹发送成功",
                    ParcelId = parcelId.ToString(),
                    Barcode = request.Barcode,
                    SentAt = DateTimeOffset.UtcNow
                });
            }
            catch (OperationCanceledException) when (cts.Token.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                // 超时
                return StatusCode(503, new ErrorResponse
                {
                    Error = "请求超时",
                    Message = "上游服务未在 10 秒内响应，可能存在网络或服务问题。"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送测试包裹失败");
            return StatusCode(500, new ErrorResponse
            {
                Error = "发送测试包裹失败",
                Message = ex.Message
            });
        }
    }
}

/// <summary>
/// 测试包裹请求
/// </summary>
public class TestParcelRequest
{
    /// <summary>
    /// 包裹条码（必填）
    /// </summary>
    /// <example>TEST-12345</example>
    public string Barcode { get; set; } = string.Empty;

    /// <summary>
    /// 包裹ID（可选，不填自动生成）
    /// </summary>
    /// <example>TEST-001</example>
    public string? ParcelId { get; set; }
}

/// <summary>
/// 测试包裹响应
/// </summary>
public class TestParcelResponse
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 响应消息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 包裹ID
    /// </summary>
    public string ParcelId { get; set; } = string.Empty;

    /// <summary>
    /// 包裹条码
    /// </summary>
    public string Barcode { get; set; } = string.Empty;

    /// <summary>
    /// 发送时间
    /// </summary>
    public DateTimeOffset SentAt { get; set; }
}

/// <summary>
/// 错误响应
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// 错误代码
    /// </summary>
    public string Error { get; set; } = string.Empty;

    /// <summary>
    /// 错误详细信息
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

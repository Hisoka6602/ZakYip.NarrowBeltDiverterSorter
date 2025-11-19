using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Communication.Upstream;
using ZakYip.NarrowBeltDiverterSorter.UpstreamContracts.Models;

namespace ZakYip.NarrowBeltDiverterSorter.E2ETests.Fakes;

/// <summary>
/// 模拟的规则引擎客户端，用于 E2E 测试
/// 可配置为成功返回、失败或超时
/// </summary>
public class FakeSortingRuleEngineClient : ISortingRuleEngineClient
{
    private readonly ILogger<FakeSortingRuleEngineClient> _logger;
    private readonly FakeSortingRuleEngineClientOptions _options;

    public FakeSortingRuleEngineClient(
        FakeSortingRuleEngineClientOptions options,
        ILogger<FakeSortingRuleEngineClient> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public bool IsConnected => _options.IsConnected;

    public event EventHandler<SortingResultMessage>? SortingResultReceived;

    public Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fake 客户端连接: {IsConnected}", _options.IsConnected);
        return Task.FromResult(_options.IsConnected);
    }

    public Task DisconnectAsync()
    {
        _logger.LogInformation("Fake 客户端断开连接");
        return Task.CompletedTask;
    }

    public async Task<bool> SendParcelCreatedAsync(ParcelCreatedMessage message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fake 客户端收到包裹创建消息: ParcelId={ParcelId}", message.ParcelId);

        if (_options.SimulateTimeout)
        {
            _logger.LogWarning("模拟超时，延迟 {TimeoutMs}ms", _options.TimeoutDelayMs);
            await Task.Delay(_options.TimeoutDelayMs, cancellationToken);
        }

        if (_options.SimulateFailure)
        {
            _logger.LogWarning("模拟失败");
            throw new InvalidOperationException("Fake 客户端模拟失败");
        }

        // 模拟成功场景：自动触发分拣结果事件
        if (_options.AutoRespondWithChuteNumber.HasValue)
        {
            _logger.LogInformation("自动返回格口 {ChuteNumber}", _options.AutoRespondWithChuteNumber.Value);
            
            // 延迟一小段时间模拟网络延迟
            await Task.Delay(_options.ResponseDelayMs, cancellationToken);

            // 触发分拣结果事件
            var result = new SortingResultMessage
            {
                ParcelId = message.ParcelId,
                ChuteNumber = _options.AutoRespondWithChuteNumber.Value,
                Success = true,
                ResultTime = DateTimeOffset.UtcNow
            };

            SortingResultReceived?.Invoke(this, result);
        }

        return true;
    }

    public Task<bool> SendDwsDataAsync(DwsDataMessage message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fake 客户端收到 DWS 数据消息: ParcelId={ParcelId}", message.ParcelId);

        if (_options.SimulateTimeout)
        {
            return Task.Delay(_options.TimeoutDelayMs, cancellationToken)
                .ContinueWith(_ => !_options.SimulateFailure, cancellationToken);
        }

        if (_options.SimulateFailure)
        {
            throw new InvalidOperationException("Fake 客户端模拟失败");
        }

        return Task.FromResult(true);
    }

    public Task<bool> SendSortingResultAsync(SortingResultMessage message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fake 客户端收到分拣结果消息: ParcelId={ParcelId}", message.ParcelId);

        if (_options.SimulateTimeout)
        {
            return Task.Delay(_options.TimeoutDelayMs, cancellationToken)
                .ContinueWith(_ => !_options.SimulateFailure, cancellationToken);
        }

        if (_options.SimulateFailure)
        {
            throw new InvalidOperationException("Fake 客户端模拟失败");
        }

        return Task.FromResult(true);
    }

    public void Dispose()
    {
        // No resources to dispose
    }
}

/// <summary>
/// Fake 客户端配置选项
/// </summary>
public class FakeSortingRuleEngineClientOptions
{
    /// <summary>
    /// 是否连接（默认 true）
    /// </summary>
    public bool IsConnected { get; set; } = true;

    /// <summary>
    /// 模拟超时（默认 false）
    /// </summary>
    public bool SimulateTimeout { get; set; } = false;

    /// <summary>
    /// 超时延迟毫秒数（默认 5000ms）
    /// </summary>
    public int TimeoutDelayMs { get; set; } = 5000;

    /// <summary>
    /// 模拟失败（默认 false）
    /// </summary>
    public bool SimulateFailure { get; set; } = false;

    /// <summary>
    /// 自动响应格口编号（设置后会自动触发 SortingResultReceived 事件）
    /// </summary>
    public int? AutoRespondWithChuteNumber { get; set; }

    /// <summary>
    /// 响应延迟毫秒数（默认 100ms）
    /// </summary>
    public int ResponseDelayMs { get; set; } = 100;

    /// <summary>
    /// 创建成功场景配置
    /// </summary>
    public static FakeSortingRuleEngineClientOptions CreateSuccessScenario(int chuteNumber)
    {
        return new FakeSortingRuleEngineClientOptions
        {
            IsConnected = true,
            SimulateTimeout = false,
            SimulateFailure = false,
            AutoRespondWithChuteNumber = chuteNumber,
            ResponseDelayMs = 50
        };
    }

    /// <summary>
    /// 创建超时场景配置
    /// </summary>
    public static FakeSortingRuleEngineClientOptions CreateTimeoutScenario()
    {
        return new FakeSortingRuleEngineClientOptions
        {
            IsConnected = true,
            SimulateTimeout = true,
            TimeoutDelayMs = 5000,
            SimulateFailure = false
        };
    }

    /// <summary>
    /// 创建失败场景配置
    /// </summary>
    public static FakeSortingRuleEngineClientOptions CreateFailureScenario()
    {
        return new FakeSortingRuleEngineClientOptions
        {
            IsConnected = true,
            SimulateTimeout = false,
            SimulateFailure = true
        };
    }
}

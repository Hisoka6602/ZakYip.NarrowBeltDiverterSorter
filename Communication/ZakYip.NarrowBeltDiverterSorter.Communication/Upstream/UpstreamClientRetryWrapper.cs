using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZakYip.NarrowBeltDiverterSorter.Core.Enums.Communication;

namespace ZakYip.NarrowBeltDiverterSorter.Communication.Upstream;

/// <summary>
/// 上游客户端连接重试包装器
/// 实现退避算法的自动重连逻辑
/// </summary>
public class UpstreamClientRetryWrapper : ISortingRuleEngineClient
{
    private readonly ISortingRuleEngineClient _innerClient;
    private readonly IOptionsMonitor<UpstreamOptions> _optionsMonitor;
    private readonly ILogger<UpstreamClientRetryWrapper> _logger;
    private readonly CancellationTokenSource _reconnectCts = new();
    private Task? _reconnectTask;
    private bool _disposed;
    private int _retryCount;

    public UpstreamClientRetryWrapper(
        ISortingRuleEngineClient innerClient,
        IOptionsMonitor<UpstreamOptions> optionsMonitor,
        ILogger<UpstreamClientRetryWrapper> logger)
    {
        _innerClient = innerClient ?? throw new ArgumentNullException(nameof(innerClient));
        _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // 监听配置变更
        _optionsMonitor.OnChange(OnOptionsChanged);
    }

    public bool IsConnected => _innerClient.IsConnected;

    public event EventHandler<ZakYip.NarrowBeltDiverterSorter.UpstreamContracts.Models.SortingResultMessage>? SortingResultReceived
    {
        add => _innerClient.SortingResultReceived += value;
        remove => _innerClient.SortingResultReceived -= value;
    }

    public event EventHandler<ZakYip.NarrowBeltDiverterSorter.UpstreamContracts.Models.ChuteAssignmentNotificationEventArgs>? ChuteAssignmentReceived
    {
        add => _innerClient.ChuteAssignmentReceived += value;
        remove => _innerClient.ChuteAssignmentReceived -= value;
    }

    public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        var options = _optionsMonitor.CurrentValue;
        
        // 如果是服务端模式或禁用模式，直接调用内部客户端
        if (options.Role == UpstreamRole.Server || options.Mode == UpstreamMode.Disabled)
        {
            return await _innerClient.ConnectAsync(cancellationToken);
        }

        // 客户端模式：启动带重试的连接逻辑
        _retryCount = 0;
        return await ConnectWithRetryAsync(cancellationToken);
    }

    private async Task<bool> ConnectWithRetryAsync(CancellationToken cancellationToken)
    {
        var options = _optionsMonitor.CurrentValue;
        var retryOptions = options.Retry;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("尝试连接到上游服务器（尝试次数: {RetryCount}）", _retryCount + 1);
                
                var connected = await _innerClient.ConnectAsync(cancellationToken);
                
                if (connected)
                {
                    _logger.LogInformation("成功连接到上游服务器");
                    _retryCount = 0; // 重置重试计数
                    return true;
                }

                // 连接失败，计算退避时间
                var backoffMs = CalculateBackoffMs(_retryCount, retryOptions);
                _logger.LogWarning("连接失败，将在 {BackoffMs}ms 后重试（尝试次数: {RetryCount}）", 
                    backoffMs, _retryCount + 1);

                _retryCount++;

                // 使用 Task.Delay 进行异步等待
                await Task.Delay(backoffMs, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("连接重试被取消");
                return false;
            }
            catch (Exception ex)
            {
                var backoffMs = CalculateBackoffMs(_retryCount, retryOptions);
                _logger.LogError(ex, "连接时发生异常，将在 {BackoffMs}ms 后重试", backoffMs);
                
                _retryCount++;
                
                try
                {
                    await Task.Delay(backoffMs, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    return false;
                }
            }

            // 如果不是无限重试模式，可以在这里添加最大重试次数检查
            if (!retryOptions.InfiniteRetry)
            {
                // 预留：未来可以添加最大重试次数限制
            }
        }

        return false;
    }

    private int CalculateBackoffMs(int retryCount, RetryOptions options)
    {
        // 计算指数退避时间：initial * (multiplier ^ retryCount)
        var backoffMs = (int)(options.InitialBackoffMs * Math.Pow(options.BackoffMultiplier, retryCount));
        
        // 确保不超过最大退避间隔
        return Math.Min(backoffMs, options.MaxBackoffMs);
    }

    private void OnOptionsChanged(UpstreamOptions newOptions)
    {
        _logger.LogInformation("检测到上游配置变更，将使用新配置重新连接");
        
        // 取消当前重连任务
        _reconnectCts.Cancel();
        
        // 断开当前连接
        _ = _innerClient.DisconnectAsync();
        
        // 如果是客户端模式，启动新的连接任务
        if (newOptions.Role == UpstreamRole.Client && newOptions.Mode != UpstreamMode.Disabled)
        {
            var newCts = new CancellationTokenSource();
            _reconnectTask = Task.Run(async () =>
            {
                await Task.Delay(500, newCts.Token); // 短暂延迟后重连
                await ConnectWithRetryAsync(newCts.Token);
            }, newCts.Token);
        }
    }

    public Task DisconnectAsync()
    {
        _reconnectCts.Cancel();
        return _innerClient.DisconnectAsync();
    }

    public async Task<bool> SendParcelCreatedAsync(
        ZakYip.NarrowBeltDiverterSorter.UpstreamContracts.Models.ParcelCreatedMessage message, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _innerClient.SendParcelCreatedAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送包裹创建消息失败 - ParcelId: {ParcelId}", message.ParcelId);
            return false;
        }
    }

    public async Task<bool> SendDwsDataAsync(
        ZakYip.NarrowBeltDiverterSorter.UpstreamContracts.Models.DwsDataMessage message, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _innerClient.SendDwsDataAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送 DWS 数据失败 - ParcelId: {ParcelId}", message.ParcelId);
            return false;
        }
    }

    public async Task<bool> SendSortingResultAsync(
        ZakYip.NarrowBeltDiverterSorter.UpstreamContracts.Models.SortingResultMessage message, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _innerClient.SendSortingResultAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送分拣结果失败 - ParcelId: {ParcelId}, ChuteNumber: {ChuteNumber}", 
                message.ParcelId, message.ChuteNumber);
            return false;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _reconnectCts.Cancel();
        _reconnectCts.Dispose();
        _innerClient.Dispose();
        _disposed = true;
    }
}

using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Communication.Upstream;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Feeding;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Parcels;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Runtime;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Upstream;
using ZakYip.NarrowBeltDiverterSorter.Execution.Upstream;
using ZakYip.NarrowBeltDiverterSorter.UpstreamContracts.Models;

namespace ZakYip.NarrowBeltDiverterSorter.Execution.Runtime;

/// <summary>
/// 包裹路由运行时实现
/// 订阅包裹创建事件，以火忘式（fire-and-forget）方式向上游请求格口分配
/// 上游响应通过推送机制异步到达，由UpstreamResponseHandler处理
/// </summary>
public class ParcelRoutingRuntime : IParcelRoutingRuntime
{
    private readonly ILogger<ParcelRoutingRuntime> _logger;
    private readonly ISortingRuleEngineClient _ruleEngineClient;
    private readonly IParcelLifecycleService _parcelLifecycleService;
    private readonly IParcelLifecycleTracker _lifecycleTracker;
    private readonly IUpstreamRequestTracker _requestTracker;
    private readonly IUpstreamRoutingConfigProvider _configProvider;
    private readonly UpstreamTimeoutChecker _timeoutChecker;
    private CancellationTokenSource? _runningCts;

    public ParcelRoutingRuntime(
        ILogger<ParcelRoutingRuntime> logger,
        ISortingRuleEngineClient ruleEngineClient,
        IParcelLifecycleService parcelLifecycleService,
        IParcelLifecycleTracker lifecycleTracker,
        IUpstreamRequestTracker requestTracker,
        IUpstreamRoutingConfigProvider configProvider,
        UpstreamTimeoutChecker timeoutChecker)
    {
        _logger = logger;
        _ruleEngineClient = ruleEngineClient;
        _parcelLifecycleService = parcelLifecycleService;
        _lifecycleTracker = lifecycleTracker;
        _requestTracker = requestTracker;
        _configProvider = configProvider;
        _timeoutChecker = timeoutChecker;
    }

    public async Task RunAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("包裹路由运行时已启动（火忘式模式）");

        _runningCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

        // 启动周期性超时检查任务
        var timeoutCheckTask = RunTimeoutCheckLoopAsync(_runningCts.Token);

        try
        {
            // 主循环保持运行
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // 正常停止
        }
        finally
        {
            _runningCts?.Cancel();
            await timeoutCheckTask;
        }

        _logger.LogInformation("包裹路由运行时已停止");
    }

    /// <summary>
    /// 超时检查循环
    /// </summary>
    private async Task RunTimeoutCheckLoopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("启动上游请求超时检查循环");

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    _timeoutChecker.CheckTimeouts();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "超时检查循环发生异常");
                }

                // 每秒检查一次
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // 正常停止
        }

        _logger.LogInformation("上游请求超时检查循环已停止");
    }

    /// <summary>
    /// 处理包裹创建事件（火忘式，立即返回）
    /// 此方法应该由事件订阅机制调用
    /// </summary>
    /// <param name="eventArgs">包裹创建事件参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task HandleParcelCreatedAsync(
        ParcelCreatedFromInfeedEventArgs eventArgs,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "开始处理包裹创建: ParcelId={ParcelId}",
                eventArgs.ParcelId.Value);

            // 1. 创建包裹（立即完成）
            _parcelLifecycleService.CreateParcel(
                eventArgs.ParcelId,
                eventArgs.Barcode,
                eventArgs.InfeedTriggerTime);

            // 2. 更新生命周期状态：已创建
            _lifecycleTracker.UpdateStatus(
                eventArgs.ParcelId,
                ParcelStatus.Created,
                ParcelFailureReason.None,
                "包裹从入口传感器创建");

            var requestedAt = DateTimeOffset.UtcNow;
            var config = _configProvider.GetCurrentOptions();
            var deadline = requestedAt.Add(config.UpstreamResultTtl);

            // 3. 记录上游请求（计算Deadline）
            _requestTracker.RecordRequest(eventArgs.ParcelId, requestedAt, deadline);

            _logger.LogDebug(
                "包裹 {ParcelId} 上游请求已记录，Deadline={Deadline}",
                eventArgs.ParcelId.Value,
                deadline);

            // 4. 火忘式发送请求到上游（不等待响应）
            var message = new ParcelCreatedMessage
            {
                ParcelId = eventArgs.ParcelId.Value,
                Barcode = eventArgs.Barcode,
                CreatedTime = eventArgs.InfeedTriggerTime
            };

            // 异步发送，不等待结果
            _ = Task.Run(async () =>
            {
                try
                {
                    bool sent = await _ruleEngineClient.SendParcelCreatedAsync(message, cancellationToken);
                    if (sent)
                    {
                        _logger.LogDebug(
                            "包裹 {ParcelId} 的上游请求已发送",
                            eventArgs.ParcelId.Value);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "包裹 {ParcelId} 的上游请求发送失败（规则引擎未连接或已禁用）",
                            eventArgs.ParcelId.Value);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "发送包裹 {ParcelId} 的上游请求时发生异常",
                        eventArgs.ParcelId.Value);
                }
            }, cancellationToken);

            _logger.LogInformation(
                "包裹 {ParcelId} 创建完成，上游请求已发送（火忘式）",
                eventArgs.ParcelId.Value);

            // 方法立即返回，不等待上游响应
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "处理包裹 {ParcelId} 创建时发生异常",
                eventArgs.ParcelId.Value);

            // 即使发生异常也要快速返回，避免阻塞
            throw;
        }
    }
}

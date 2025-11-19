using System.Threading;
using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Feeding;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Parcels;

namespace ZakYip.NarrowBeltDiverterSorter.Execution.Feeding;

/// <summary>
/// 供包背压控制器实现
/// 基于在途包裹数和上游等待数决策供包策略
/// </summary>
public class FeedingBackpressureController : IFeedingBackpressureController
{
    private readonly IFeedingCapacityOptionsRepository _optionsRepository;
    private readonly IParcelLifecycleTracker _lifecycleTracker;
    private readonly ILogger<FeedingBackpressureController> _logger;
    
    private long _throttleCount;
    private long _pauseCount;
    private FeedingCapacityOptions? _cachedOptions;
    private DateTime _lastOptionsLoadTime = DateTime.MinValue;
    private static readonly TimeSpan OptionsRefreshInterval = TimeSpan.FromSeconds(5);

    public FeedingBackpressureController(
        IFeedingCapacityOptionsRepository optionsRepository,
        IParcelLifecycleTracker lifecycleTracker,
        ILogger<FeedingBackpressureController> logger)
    {
        _optionsRepository = optionsRepository ?? throw new ArgumentNullException(nameof(optionsRepository));
        _lifecycleTracker = lifecycleTracker ?? throw new ArgumentNullException(nameof(lifecycleTracker));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public FeedingDecisionResult CheckFeedingAllowed()
    {
        // 刷新配置（带缓存）
        var options = GetOrRefreshOptions();

        // 获取当前负载信息
        var inFlightCount = _lifecycleTracker.GetInFlightCount();
        var upstreamPendingCount = _lifecycleTracker.GetUpstreamPendingCount();

        // 检查上游等待数
        if (upstreamPendingCount >= options.MaxUpstreamPendingRequests)
        {
            var reason = $"上游等待数 {upstreamPendingCount} 已达到限制 {options.MaxUpstreamPendingRequests}";
            
            return options.ThrottleMode switch
            {
                FeedingThrottleMode.Pause => new FeedingDecisionResult
                {
                    Decision = FeedingDecision.Reject,
                    Reason = reason,
                    CurrentInFlightCount = inFlightCount,
                    CurrentUpstreamPendingCount = upstreamPendingCount
                },
                FeedingThrottleMode.SlowDown => new FeedingDecisionResult
                {
                    Decision = FeedingDecision.Delay,
                    Reason = reason,
                    CurrentInFlightCount = inFlightCount,
                    CurrentUpstreamPendingCount = upstreamPendingCount,
                    SuggestedDelayMs = 1000
                },
                _ => new FeedingDecisionResult
                {
                    Decision = FeedingDecision.Allow,
                    Reason = reason + "（仅告警，未启用节流）",
                    CurrentInFlightCount = inFlightCount,
                    CurrentUpstreamPendingCount = upstreamPendingCount
                }
            };
        }

        // 检查在途包裹数
        if (inFlightCount >= options.MaxInFlightParcels)
        {
            var reason = $"在途包裹数 {inFlightCount} 已达到限制 {options.MaxInFlightParcels}";
            
            return options.ThrottleMode switch
            {
                FeedingThrottleMode.Pause => new FeedingDecisionResult
                {
                    Decision = FeedingDecision.Reject,
                    Reason = reason,
                    CurrentInFlightCount = inFlightCount,
                    CurrentUpstreamPendingCount = upstreamPendingCount
                },
                FeedingThrottleMode.SlowDown => new FeedingDecisionResult
                {
                    Decision = FeedingDecision.Delay,
                    Reason = reason,
                    CurrentInFlightCount = inFlightCount,
                    CurrentUpstreamPendingCount = upstreamPendingCount,
                    SuggestedDelayMs = 500
                },
                _ => new FeedingDecisionResult
                {
                    Decision = FeedingDecision.Allow,
                    Reason = reason + "（仅告警，未启用节流）",
                    CurrentInFlightCount = inFlightCount,
                    CurrentUpstreamPendingCount = upstreamPendingCount
                }
            };
        }

        // 检查是否处于恢复阈值以下（用于降速恢复）
        var recoveryThreshold = options.GetRecoveryThreshold();
        if (inFlightCount < recoveryThreshold)
        {
            return new FeedingDecisionResult
            {
                Decision = FeedingDecision.Allow,
                Reason = "负载正常",
                CurrentInFlightCount = inFlightCount,
                CurrentUpstreamPendingCount = upstreamPendingCount
            };
        }

        // 接近限制但未超过，根据模式决定
        if (inFlightCount >= recoveryThreshold && options.ThrottleMode == FeedingThrottleMode.SlowDown)
        {
            return new FeedingDecisionResult
            {
                Decision = FeedingDecision.Delay,
                Reason = $"在途包裹数 {inFlightCount} 接近限制，预防性降速",
                CurrentInFlightCount = inFlightCount,
                CurrentUpstreamPendingCount = upstreamPendingCount,
                SuggestedDelayMs = 300
            };
        }

        // 默认允许
        return new FeedingDecisionResult
        {
            Decision = FeedingDecision.Allow,
            Reason = "负载正常",
            CurrentInFlightCount = inFlightCount,
            CurrentUpstreamPendingCount = upstreamPendingCount
        };
    }

    /// <inheritdoc/>
    public void RecordThrottleEvent()
    {
        Interlocked.Increment(ref _throttleCount);
    }

    /// <inheritdoc/>
    public void RecordPauseEvent()
    {
        Interlocked.Increment(ref _pauseCount);
    }

    /// <inheritdoc/>
    public long GetThrottleCount()
    {
        return Interlocked.Read(ref _throttleCount);
    }

    /// <inheritdoc/>
    public long GetPauseCount()
    {
        return Interlocked.Read(ref _pauseCount);
    }

    /// <inheritdoc/>
    public void ResetCounters()
    {
        Interlocked.Exchange(ref _throttleCount, 0);
        Interlocked.Exchange(ref _pauseCount, 0);
    }

    private FeedingCapacityOptions GetOrRefreshOptions()
    {
        var now = DateTime.UtcNow;
        
        // 如果缓存有效，直接返回
        if (_cachedOptions != null && (now - _lastOptionsLoadTime) < OptionsRefreshInterval)
        {
            return _cachedOptions;
        }

        try
        {
            // 同步加载配置（在热路径上，避免异步开销）
            _cachedOptions = _optionsRepository.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();
            _lastOptionsLoadTime = now;
            return _cachedOptions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载供包容量配置失败，使用默认配置");
            
            // 失败时使用默认配置
            if (_cachedOptions == null)
            {
                _cachedOptions = new FeedingCapacityOptions();
            }
            
            return _cachedOptions;
        }
    }
}

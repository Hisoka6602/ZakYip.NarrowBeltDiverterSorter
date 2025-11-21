using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.MainLine;

namespace ZakYip.NarrowBeltDiverterSorter.Execution.Mainline;

/// <summary>
/// 主线速度提供者实现
/// 提供平滑后的速度值和稳定状态判断
/// </summary>
public class MainLineSpeedProvider : IMainLineSpeedProvider
{
    private readonly IMainLineFeedbackPort _feedbackPort;
    private readonly MainLineControlOptions _options;
    private readonly Queue<decimal> _speedSamples;
    private readonly int _smoothingWindowSize;
    private DateTime _stableStartTime;
    private bool _wasStable;
    private readonly object _lock = new();
    private readonly IPanelIoCoordinator? _panelIoCoordinator;
    private readonly ILogger<MainLineSpeedProvider>? _logger;
    
    // 首次稳速状态跟踪
    private bool _hasEverBeenStable;
    private bool _firstStableLinkageTriggered;

    public MainLineSpeedProvider(
        IMainLineFeedbackPort feedbackPort,
        IOptions<MainLineControlOptions> options,
        IPanelIoCoordinator? panelIoCoordinator = null,
        ILogger<MainLineSpeedProvider>? logger = null)
    {
        _feedbackPort = feedbackPort;
        _options = options.Value;
        _smoothingWindowSize = CalculateSmoothingWindowSize(_options.LoopPeriod);
        _speedSamples = new Queue<decimal>(_smoothingWindowSize);
        _stableStartTime = DateTime.MinValue;
        _wasStable = false;
        _panelIoCoordinator = panelIoCoordinator;
        _logger = logger;
        _hasEverBeenStable = false;
        _firstStableLinkageTriggered = false;
    }

    /// <inheritdoc/>
    public decimal CurrentMmps
    {
        get
        {
            lock (_lock)
            {
                UpdateSpeedSamples();
                return CalculateSmoothedSpeed();
            }
        }
    }

    /// <inheritdoc/>
    public bool IsSpeedStable
    {
        get
        {
            lock (_lock)
            {
                UpdateSpeedSamples();
                var currentSpeed = CalculateSmoothedSpeed();
                var targetSpeed = _options.TargetSpeedMmps;
                var error = Math.Abs(currentSpeed - targetSpeed);
                
                var isCurrentlyStable = error <= _options.StableDeadbandMmps;
                
                if (isCurrentlyStable && !_wasStable)
                {
                    // 刚进入稳定状态
                    _stableStartTime = DateTime.Now;
                    _wasStable = true;
                }
                else if (!isCurrentlyStable && _wasStable)
                {
                    // 离开稳定状态
                    _wasStable = false;
                    _stableStartTime = DateTime.MinValue;
                    
                    // 如果之前已经稳定过，触发"稳速后不稳速"联动 IO
                    if (_hasEverBeenStable && _panelIoCoordinator != null)
                    {
                        // 异步触发但不等待，避免阻塞
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await _panelIoCoordinator.ExecuteUnstableAfterStableLinkageAsync();
                            }
                            catch (Exception ex)
                            {
                                _logger?.LogError(ex, "触发稳速后不稳速联动 IO 失败");
                            }
                        });
                    }
                }

                if (!isCurrentlyStable)
                {
                    return false;
                }

                // 检查稳定持续时间
                var stableDuration = DateTime.Now - _stableStartTime;
                var isStableEnough = stableDuration >= _options.StableHold;
                
                // 如果达到稳定条件且是首次稳定，触发"首次稳速"联动 IO
                if (isStableEnough && !_hasEverBeenStable)
                {
                    _hasEverBeenStable = true;
                    
                    if (_panelIoCoordinator != null && !_firstStableLinkageTriggered)
                    {
                        _firstStableLinkageTriggered = true;
                        // 异步触发但不等待，避免阻塞
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await _panelIoCoordinator.ExecuteFirstStableSpeedLinkageAsync();
                            }
                            catch (Exception ex)
                            {
                                _logger?.LogError(ex, "触发首次稳速联动 IO 失败");
                            }
                        });
                    }
                }
                
                return isStableEnough;
            }
        }
    }

    /// <inheritdoc/>
    public TimeSpan StableDuration
    {
        get
        {
            lock (_lock)
            {
                if (!_wasStable)
                {
                    return TimeSpan.Zero;
                }

                return DateTime.Now - _stableStartTime;
            }
        }
    }

    /// <summary>
    /// 更新速度采样
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateSpeedSamples()
    {
        var currentSpeed = (decimal)_feedbackPort.GetCurrentSpeed();
        
        _speedSamples.Enqueue(currentSpeed);
        
        // 维持固定窗口大小
        while (_speedSamples.Count > _smoothingWindowSize)
        {
            _speedSamples.Dequeue();
        }
    }

    /// <summary>
    /// 计算平滑后的速度（移动平均）
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private decimal CalculateSmoothedSpeed()
    {
        if (_speedSamples.Count == 0)
        {
            return 0m;
        }

        return _speedSamples.Average();
    }

    /// <summary>
    /// 计算平滑窗口大小
    /// 基于控制周期，使用约1秒的数据
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int CalculateSmoothingWindowSize(TimeSpan loopPeriod)
    {
        var samplesPerSecond = 1.0 / loopPeriod.TotalSeconds;
        // 至少3个样本，最多50个样本
        return Math.Clamp((int)samplesPerSecond, 3, 50);
    }

    /// <summary>
    /// 重置平滑滤波器
    /// </summary>
    public void ResetSmoothing()
    {
        lock (_lock)
        {
            _speedSamples.Clear();
            _stableStartTime = DateTime.MinValue;
            _wasStable = false;
            _hasEverBeenStable = false;
            _firstStableLinkageTriggered = false;
        }
    }
}

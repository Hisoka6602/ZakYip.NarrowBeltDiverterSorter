using System.Runtime.CompilerServices;
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

    public MainLineSpeedProvider(
        IMainLineFeedbackPort feedbackPort,
        IOptions<MainLineControlOptions> options)
    {
        _feedbackPort = feedbackPort;
        _options = options.Value;
        _smoothingWindowSize = CalculateSmoothingWindowSize(_options.LoopPeriod);
        _speedSamples = new Queue<decimal>(_smoothingWindowSize);
        _stableStartTime = DateTime.MinValue;
        _wasStable = false;
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
                }

                if (!isCurrentlyStable)
                {
                    return false;
                }

                // 检查稳定持续时间
                var stableDuration = DateTime.Now - _stableStartTime;
                return stableDuration >= _options.StableHold;
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
        }
    }
}

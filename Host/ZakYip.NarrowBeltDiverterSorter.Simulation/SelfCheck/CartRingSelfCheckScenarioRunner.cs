using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.MainLine;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Topology;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Tracking;
using ZakYip.NarrowBeltDiverterSorter.Core.SelfCheck;

namespace ZakYip.NarrowBeltDiverterSorter.Simulation.SelfCheck;

/// <summary>
/// 小车环自检仿真场景运行器
/// </summary>
public class CartRingSelfCheckScenarioRunner
{
    private readonly ILogger<CartRingSelfCheckScenarioRunner> _logger;
    private readonly ICartRingBuilder _cartRingBuilder;
    private readonly ICartPositionTracker _cartPositionTracker;
    private readonly IMainLineSpeedProvider _speedProvider;
    private readonly IMainLineControlService _mainLineControl;
    private readonly ITrackTopology _trackTopology;
    private readonly CartSelfCheckEventCollector _eventCollector;
    private readonly ICartRingSelfCheckService _selfCheckService;
    private readonly CartRingSelfCheckOptions _selfCheckOptions;

    public CartRingSelfCheckScenarioRunner(
        ILogger<CartRingSelfCheckScenarioRunner> logger,
        ICartRingBuilder cartRingBuilder,
        ICartPositionTracker cartPositionTracker,
        IMainLineSpeedProvider speedProvider,
        IMainLineControlService mainLineControl,
        ITrackTopology trackTopology,
        CartSelfCheckEventCollector eventCollector,
        ICartRingSelfCheckService selfCheckService,
        CartRingSelfCheckOptions selfCheckOptions)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cartRingBuilder = cartRingBuilder ?? throw new ArgumentNullException(nameof(cartRingBuilder));
        _cartPositionTracker = cartPositionTracker ?? throw new ArgumentNullException(nameof(cartPositionTracker));
        _speedProvider = speedProvider ?? throw new ArgumentNullException(nameof(speedProvider));
        _mainLineControl = mainLineControl ?? throw new ArgumentNullException(nameof(mainLineControl));
        _trackTopology = trackTopology ?? throw new ArgumentNullException(nameof(trackTopology));
        _eventCollector = eventCollector ?? throw new ArgumentNullException(nameof(eventCollector));
        _selfCheckService = selfCheckService ?? throw new ArgumentNullException(nameof(selfCheckService));
        _selfCheckOptions = selfCheckOptions ?? throw new ArgumentNullException(nameof(selfCheckOptions));
    }

    /// <summary>
    /// 运行自检场景
    /// </summary>
    public async Task<CartRingSelfCheckResult> RunAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("【小车环自检】开始运行自检场景");

        // 1. 启动主线并等待速度稳定
        _logger.LogInformation("【小车环自检】启动主线...");
        await _mainLineControl.StartAsync(cancellationToken);
        await WaitForMainLineStableAsync(cancellationToken);

        // 2. 等待小车环构建完成
        _logger.LogInformation("【小车环自检】等待小车环构建...");
        await WaitForCartRingReadyAsync(cancellationToken);

        // 3. 开始收集事件
        _logger.LogInformation("【小车环自检】开始收集小车通过事件...");
        _eventCollector.StartCollecting();

        // 4. 运行足够长的时间以收集至少MinCompleteRings圈的数据
        var samplingDuration = CalculateSamplingDuration();
        _logger.LogInformation(
            "【小车环自检】运行采样，持续 {Duration:F1} 秒（至少 {Rings} 圈）",
            samplingDuration.TotalSeconds,
            _selfCheckOptions.MinCompleteRings);
        
        await Task.Delay(samplingDuration, cancellationToken);

        // 5. 停止收集事件
        _eventCollector.StopCollecting();
        var collectedEvents = _eventCollector.GetCollectedEvents();
        _logger.LogInformation("【小车环自检】收集完成，共 {Count} 个事件", collectedEvents.Count);

        // 6. 停止主线
        await _mainLineControl.StopAsync(cancellationToken);

        // 7. 执行自检分析
        _logger.LogInformation("【小车环自检】执行自检分析...");
        var topologySnapshot = new TrackTopologySnapshot
        {
            CartCount = _trackTopology.CartCount,
            CartSpacingMm = _trackTopology.CartSpacingMm,
            RingTotalLengthMm = _trackTopology.RingTotalLengthMm,
            ChuteCount = _trackTopology.ChuteCount,
            ChuteWidthMm = _trackTopology.ChuteWidthMm,
            CartWidthMm = _trackTopology.CartWidthMm,
            TrackLengthMm = _trackTopology.TrackLengthMm
        };

        var result = _selfCheckService.RunAnalysis(collectedEvents, topologySnapshot);

        // 8. 输出结果
        LogSelfCheckResult(result);

        return result;
    }

    /// <summary>
    /// 等待主线启动并稳定
    /// </summary>
    private async Task WaitForMainLineStableAsync(CancellationToken cancellationToken)
    {
        const int maxWaitSeconds = 10;
        var timeout = DateTime.UtcNow.AddSeconds(maxWaitSeconds);

        while (DateTime.UtcNow < timeout && !cancellationToken.IsCancellationRequested)
        {
            if (_mainLineControl.IsRunning && _speedProvider.IsSpeedStable)
            {
                _logger.LogInformation(
                    "【小车环自检】主线已启动并稳定，当前速度: {Speed:F1} mm/s",
                    _speedProvider.CurrentMmps);
                return;
            }

            await Task.Delay(100, cancellationToken);
        }

        _logger.LogWarning("【小车环自检】主线未能在 {MaxWaitSeconds} 秒内稳定，继续执行", maxWaitSeconds);
    }

    /// <summary>
    /// 等待小车环构建完成
    /// </summary>
    private async Task WaitForCartRingReadyAsync(CancellationToken cancellationToken)
    {
        const int maxWaitSeconds = 90;
        var timeout = DateTime.UtcNow.AddSeconds(maxWaitSeconds);

        while (DateTime.UtcNow < timeout && !cancellationToken.IsCancellationRequested)
        {
            var snapshot = _cartRingBuilder.CurrentSnapshot;
            if (snapshot != null && _cartPositionTracker.IsRingReady)
            {
                _logger.LogInformation(
                    "【小车环自检】小车环已就绪 - 小车数量: {CartCount}, 零点车ID: {ZeroCartId}",
                    snapshot.RingLength.Value,
                    snapshot.ZeroCartId.Value);
                return;
            }

            await Task.Delay(100, cancellationToken);
        }

        throw new TimeoutException($"小车环未能在 {maxWaitSeconds} 秒内完成构建并就绪");
    }

    /// <summary>
    /// 计算采样时长
    /// 基于小车环长度和速度，确保收集足够多圈的数据
    /// </summary>
    private TimeSpan CalculateSamplingDuration()
    {
        // 一圈的时间 = 环总长 / 速度
        var ringLengthMm = _trackTopology.RingTotalLengthMm;
        var speedMmps = _speedProvider.CurrentMmps;

        if (speedMmps <= 0)
        {
            // 如果速度未知，使用配置的最小采样时长
            return TimeSpan.FromSeconds(_selfCheckOptions.MinSamplingDurationSeconds);
        }

        var timePerRingSeconds = (double)(ringLengthMm / speedMmps);
        var totalSeconds = timePerRingSeconds * _selfCheckOptions.MinCompleteRings;

        // 确保不低于最小采样时长
        totalSeconds = Math.Max(totalSeconds, _selfCheckOptions.MinSamplingDurationSeconds);

        return TimeSpan.FromSeconds(totalSeconds);
    }

    /// <summary>
    /// 输出自检结果日志
    /// </summary>
    private void LogSelfCheckResult(CartRingSelfCheckResult result)
    {
        _logger.LogInformation("【小车环自检】分析结果：");
        _logger.LogInformation("  配置小车数: {Expected} 辆", result.ExpectedCartCount);
        _logger.LogInformation("  检测小车数: {Measured} 辆", result.MeasuredCartCount);
        _logger.LogInformation("  配置节距: {Expected:F1} mm", result.ExpectedPitchMm);
        _logger.LogInformation("  估算节距: {Measured:F1} mm", result.MeasuredPitchMm);
        _logger.LogInformation("  数车结果: {Result}", result.IsCartCountMatched ? "✓ 通过" : "✗ 不匹配");
        _logger.LogInformation(
            "  节距结果: {Result}",
            result.IsPitchWithinTolerance
                ? $"✓ 在误差范围内 (阈值: {_selfCheckOptions.PitchTolerancePercent * 100:F1}%)"
                : $"✗ 超出误差范围 (阈值: {_selfCheckOptions.PitchTolerancePercent * 100:F1}%)");
    }
}

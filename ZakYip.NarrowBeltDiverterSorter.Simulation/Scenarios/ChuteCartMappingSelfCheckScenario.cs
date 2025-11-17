using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.MainLine;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Topology;
using ZakYip.NarrowBeltDiverterSorter.Core.SelfCheck;
using ZakYip.NarrowBeltDiverterSorter.Simulation.Fakes;

namespace ZakYip.NarrowBeltDiverterSorter.Simulation.Scenarios;

/// <summary>
/// 格口-小车映射自检场景运行器
/// 基于主驱动线运动验证格口与小车的映射关系
/// </summary>
public class ChuteCartMappingSelfCheckScenario
{
    private readonly ILogger<ChuteCartMappingSelfCheckScenario> _logger;
    private readonly IMainLineControlService _mainLineControl;
    private readonly IMainLineSpeedProvider _speedProvider;
    private readonly FakeMainLineFeedbackPort _mainLineFeedback;
    private readonly ITrackTopology _trackTopology;
    private readonly SimulationConfiguration _config;
    private readonly IChuteCartMappingSelfCheckService _selfCheckService;
    private readonly ChuteCartMappingSelfCheckOptions _selfCheckOptions;

    private readonly List<ChutePassEventArgs> _collectedEvents = new();
    private int _currentCartIndex = 0;
    private int _originPassCount = 0;

    public ChuteCartMappingSelfCheckScenario(
        ILogger<ChuteCartMappingSelfCheckScenario> logger,
        IMainLineControlService mainLineControl,
        IMainLineSpeedProvider speedProvider,
        FakeMainLineFeedbackPort mainLineFeedback,
        ITrackTopology trackTopology,
        SimulationConfiguration config,
        IChuteCartMappingSelfCheckService selfCheckService,
        ChuteCartMappingSelfCheckOptions selfCheckOptions)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mainLineControl = mainLineControl ?? throw new ArgumentNullException(nameof(mainLineControl));
        _speedProvider = speedProvider ?? throw new ArgumentNullException(nameof(speedProvider));
        _mainLineFeedback = mainLineFeedback ?? throw new ArgumentNullException(nameof(mainLineFeedback));
        _trackTopology = trackTopology ?? throw new ArgumentNullException(nameof(trackTopology));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _selfCheckService = selfCheckService ?? throw new ArgumentNullException(nameof(selfCheckService));
        _selfCheckOptions = selfCheckOptions ?? throw new ArgumentNullException(nameof(selfCheckOptions));
    }

    /// <summary>
    /// 运行格口-小车映射自检场景
    /// </summary>
    public async Task<ChuteCartMappingSelfCheckResult> RunAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("【格口-小车映射自检】开始运行自检场景");
        _logger.LogInformation("  格口数量: {ChuteCount}", _trackTopology.ChuteCount);
        _logger.LogInformation("  小车数量: {CartCount}", _trackTopology.CartCount);
        _logger.LogInformation("  格口宽度: {ChuteWidth} mm", _trackTopology.ChuteWidthMm);
        _logger.LogInformation("  小车宽度: {CartWidth} mm", _trackTopology.CartWidthMm);
        _logger.LogInformation("  主线长度: {TrackLength} mm", _trackTopology.TrackLengthMm);
        _logger.LogInformation("  自检圈数: {LoopCount}", _selfCheckOptions.LoopCount);

        // 1. 启动主线并等待速度稳定
        _logger.LogInformation("【格口-小车映射自检】启动主线...");
        await _mainLineControl.StartAsync(cancellationToken);
        await WaitForMainLineStableAsync(cancellationToken);

        // 2. 模拟小车运动并采集格口IO事件
        _logger.LogInformation("【格口-小车映射自检】开始采集格口IO触发事件...");
        _collectedEvents.Clear();
        _currentCartIndex = 0;
        _originPassCount = 0;

        await RunSimulationLoopsAsync(cancellationToken);

        // 3. 停止主线
        _logger.LogInformation("【格口-小车映射自检】采集完成，共 {EventCount} 个事件", _collectedEvents.Count);
        await _mainLineControl.StopAsync(cancellationToken);

        // 4. 执行自检分析
        _logger.LogInformation("【格口-小车映射自检】执行自检分析...");
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

        var result = _selfCheckService.Analyze(_collectedEvents, topologySnapshot, _selfCheckOptions);

        // 5. 输出结果
        LogSelfCheckResult(result);
        WriteSelfCheckReportToFile(result);

        return result;
    }

    /// <summary>
    /// 等待主线速度稳定
    /// </summary>
    private async Task WaitForMainLineStableAsync(CancellationToken cancellationToken)
    {
        var timeout = TimeSpan.FromSeconds(10);
        var startTime = DateTime.UtcNow;

        while (DateTime.UtcNow - startTime < timeout)
        {
            var currentSpeed = _speedProvider.CurrentMmps;
            if (currentSpeed >= (decimal)_config.MainLineSpeedMmPerSec * 0.95m)
            {
                _logger.LogInformation("【格口-小车映射自检】主线速度已稳定: {Speed:F2} mm/s", currentSpeed);
                return;
            }

            await Task.Delay(100, cancellationToken);
        }

        _logger.LogWarning("【格口-小车映射自检】主线速度未完全稳定，继续执行");
    }

    /// <summary>
    /// 运行仿真循环，采集N圈的事件
    /// </summary>
    private async Task RunSimulationLoopsAsync(CancellationToken cancellationToken)
    {
        while (_originPassCount < _selfCheckOptions.LoopCount && !cancellationToken.IsCancellationRequested)
        {
            var currentSpeed = _mainLineFeedback.GetCurrentSpeed();

            if (currentSpeed > 0)
            {
                // 计算小车通过原点的时间间隔
                var cartPassingIntervalMs = (double)(_trackTopology.CartSpacingMm / (decimal)currentSpeed * 1000);

                // 当前小车的ID
                var currentCartId = _currentCartIndex;

                // 检查该小车是否经过任何格口
                SimulateChuteIoTriggers(currentCartId, currentSpeed);

                // 如果是0号车，记录圈数
                if (currentCartId == 0)
                {
                    _originPassCount++;
                    _logger.LogDebug("【格口-小车映射自检】0号车通过原点 - 完成第 {Loop} 圈", _originPassCount);
                }

                // 移动到下一个小车
                _currentCartIndex = (_currentCartIndex + 1) % _trackTopology.CartCount;

                await Task.Delay((int)cartPassingIntervalMs, cancellationToken);
            }
            else
            {
                // 主线停止时，等待
                await Task.Delay(100, cancellationToken);
            }
        }
    }

    /// <summary>
    /// 模拟格口IO触发
    /// 当小车位置经过格口中心时，生成ChutePassEventArgs事件
    /// </summary>
    private void SimulateChuteIoTriggers(int cartId, double currentSpeed)
    {
        // 计算小车当前位置（基于小车ID和节距）
        var cartPositionMm = cartId * _trackTopology.CartSpacingMm;

        // 遍历所有格口，检查小车是否经过
        for (int chuteId = 1; chuteId <= _trackTopology.ChuteCount; chuteId++)
        {
            // 计算格口中心位置（假设格口连续排列，从位置0开始）
            var chutePositionMm = _trackTopology.ChuteWidthMm * (chuteId - 1);

            // 计算小车与格口的距离（考虑环形拓扑）
            var distance = CalculateRingDistance(cartPositionMm, chutePositionMm, _trackTopology.RingTotalLengthMm);

            // 如果小车在格口的触发窗口内（使用小车宽度的一半作为触发窗口）
            var triggerWindowMm = _trackTopology.CartWidthMm / 2;

            if (distance <= triggerWindowMm)
            {
                // 生成格口IO触发事件
                var passEvent = new ChutePassEventArgs
                {
                    ChuteId = chuteId,
                    CartId = cartId,
                    TriggeredAt = DateTimeOffset.UtcNow,
                    LineSpeedMmps = (decimal)currentSpeed
                };

                _collectedEvents.Add(passEvent);

                _logger.LogTrace(
                    "【格口IO触发】格口 {ChuteId} 检测到小车 {CartId}（距离: {Distance:F1} mm）",
                    chuteId, cartId, distance);
            }
        }
    }

    /// <summary>
    /// 计算环形拓扑中两个位置的最短距离
    /// </summary>
    private decimal CalculateRingDistance(decimal position1, decimal position2, decimal ringLength)
    {
        var diff = Math.Abs(position1 - position2);
        var wrapAroundDiff = ringLength - diff;
        return Math.Min(diff, wrapAroundDiff);
    }

    /// <summary>
    /// 输出自检结果到日志
    /// </summary>
    private void LogSelfCheckResult(ChuteCartMappingSelfCheckResult result)
    {
        _logger.LogInformation("========================================");
        _logger.LogInformation("【格口-小车映射自检】自检结果报告");
        _logger.LogInformation("========================================");
        _logger.LogInformation("  格口数量:          {ChuteCount}", result.ChuteCount);
        _logger.LogInformation("  小车数量:          {CartCount}", result.CartCount);
        _logger.LogInformation("  自检圈数:          {LoopCount}", _selfCheckOptions.LoopCount);
        _logger.LogInformation("");

        foreach (var item in result.ChuteItems)
        {
            var statusIcon = item.IsPassed ? "✓" : "✗";
            var observedStr = string.Join(",", item.ObservedCartIds);
            _logger.LogInformation(
                "  格口 {ChuteId,3}: 理论小车: {Expected,3}, 观测: [{Observed}]      结果: {Status} {Result}",
                item.ChuteId,
                item.ExpectedCartId,
                observedStr,
                statusIcon,
                item.IsPassed ? "通过" : "失败");
        }

        _logger.LogInformation("");
        _logger.LogInformation("  汇总结果:          {Status} {Result}",
            result.IsAllPassed ? "✓" : "✗",
            result.IsAllPassed ? "全部格口在容差范围内" : "部分格口超出容差范围");
        _logger.LogInformation("========================================");
    }

    /// <summary>
    /// 将自检报告写入文件
    /// </summary>
    private void WriteSelfCheckReportToFile(ChuteCartMappingSelfCheckResult result)
    {
        try
        {
            var reportFileName = $"chute-cart-mapping-self-check-{DateTime.Now:yyyyMMdd-HHmmss}.log";
            var reportLines = new List<string>
            {
                "========================================",
                "【格口-小车映射自检】自检结果报告",
                "========================================",
                $"  格口数量:          {result.ChuteCount}",
                $"  小车数量:          {result.CartCount}",
                $"  自检圈数:          {_selfCheckOptions.LoopCount}",
                ""
            };

            foreach (var item in result.ChuteItems)
            {
                var statusIcon = item.IsPassed ? "✓" : "✗";
                var observedStr = string.Join(",", item.ObservedCartIds);
                reportLines.Add(
                    $"  格口 {item.ChuteId,3}: 理论小车: {item.ExpectedCartId,3}, 观测: [{observedStr}]      结果: {statusIcon} {(item.IsPassed ? "通过" : "失败")}");
            }

            reportLines.Add("");
            reportLines.Add(
                $"  汇总结果:          {(result.IsAllPassed ? "✓" : "✗")} {(result.IsAllPassed ? "全部格口在容差范围内" : "部分格口超出容差范围")}");
            reportLines.Add("========================================");

            File.WriteAllLines(reportFileName, reportLines);

            _logger.LogInformation("【格口-小车映射自检】自检报告已写入文件: {FileName}", reportFileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "【格口-小车映射自检】写入报告文件失败");
        }
    }
}

using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Carts;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Feeding;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.MainLine;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Parcels;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Sorting;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Tracking;
using ZakYip.NarrowBeltDiverterSorter.Simulation.Fakes;
using ZakYip.NarrowBeltDiverterSorter.Simulation.Options;
using ZakYip.NarrowBeltDiverterSorter.UpstreamContracts.Models;

namespace ZakYip.NarrowBeltDiverterSorter.Simulation.Scenarios;

/// <summary>
/// 长时间高负载分拣稳定性仿真场景。
/// </summary>
public class LongRunHighLoadSortingScenario
{
    private readonly LongRunLoadTestOptions _options;
    private readonly ILogger<LongRunHighLoadSortingScenario> _logger;
    private readonly ICartRingBuilder _cartRingBuilder;
    private readonly ICartPositionTracker _cartPositionTracker;
    private readonly IParcelLifecycleService _parcelLifecycleService;
    private readonly ICartLifecycleService _cartLifecycleService;
    private readonly IParcelLoadPlanner _loadPlanner;
    private readonly ISortingPlanner _sortingPlanner;
    private readonly IMainLineSpeedProvider _speedProvider;
    private readonly IMainLineControlService _mainLineControl;
    private readonly IUpstreamSortingApiClient _upstreamClient;
    private readonly IChuteConfigProvider _chuteConfigProvider;
    private readonly FakeOriginSensorPort _originSensor;
    private readonly FakeInfeedSensorPort _infeedSensor;
    private readonly FakeChuteTransmitterPort _chuteTransmitter;
    private readonly InfeedLayoutOptions _infeedLayout;
    private readonly Core.Domain.Topology.ITrackTopology _trackTopology;
    private readonly ParcelTimelineRecorder _timelineRecorder;
    private readonly Random _random = new Random();

    private int _generatedCount = 0;

    public LongRunHighLoadSortingScenario(
        LongRunLoadTestOptions options,
        ILogger<LongRunHighLoadSortingScenario> logger,
        ICartRingBuilder cartRingBuilder,
        ICartPositionTracker cartPositionTracker,
        IParcelLifecycleService parcelLifecycleService,
        ICartLifecycleService cartLifecycleService,
        IParcelLoadPlanner loadPlanner,
        ISortingPlanner sortingPlanner,
        IMainLineSpeedProvider speedProvider,
        IMainLineControlService mainLineControl,
        IUpstreamSortingApiClient upstreamClient,
        IChuteConfigProvider chuteConfigProvider,
        FakeOriginSensorPort originSensor,
        FakeInfeedSensorPort infeedSensor,
        FakeChuteTransmitterPort chuteTransmitter,
        InfeedLayoutOptions infeedLayout,
        Core.Domain.Topology.ITrackTopology trackTopology,
        ParcelTimelineRecorder timelineRecorder)
    {
        _options = options;
        _logger = logger;
        _cartRingBuilder = cartRingBuilder;
        _cartPositionTracker = cartPositionTracker;
        _parcelLifecycleService = parcelLifecycleService;
        _cartLifecycleService = cartLifecycleService;
        _loadPlanner = loadPlanner;
        _sortingPlanner = sortingPlanner;
        _speedProvider = speedProvider;
        _mainLineControl = mainLineControl;
        _upstreamClient = upstreamClient;
        _chuteConfigProvider = chuteConfigProvider;
        _originSensor = originSensor;
        _infeedSensor = infeedSensor;
        _chuteTransmitter = chuteTransmitter;
        _infeedLayout = infeedLayout;
        _trackTopology = trackTopology;
        _timelineRecorder = timelineRecorder;
    }

    /// <summary>
    /// 运行长时间高负载仿真场景。
    /// </summary>
    public async Task<SimulationReport> RunAsync(CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.Now;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        _logger.LogInformation("开始长时间高负载分拣稳定性仿真，目标包裹数: {TargetCount}", _options.TargetParcelCount);

        // 步骤 1: 等待主线控制启动并稳定
        _logger.LogInformation("步骤 1/4: 等待主线控制启动并稳定");
        await WaitForMainLineStableAsync(cancellationToken);

        // 步骤 2: 等待小车环就绪
        _logger.LogInformation("步骤 2/4: 等待小车环构建完成");
        var cartRingStartTime = DateTime.Now;
        await WaitForCartRingReadyAsync(cancellationToken);
        var cartRingWarmupDuration = (DateTime.Now - cartRingStartTime).TotalSeconds;

        var cartRingSnapshot = _cartRingBuilder.CurrentSnapshot;
        if (cartRingSnapshot == null)
        {
            throw new InvalidOperationException("小车环构建失败");
        }

        _logger.LogInformation(
            "[CartRing] 小车环已就绪，长度={CartCount}，节距={SpacingMm}mm，耗时={WarmupDuration:F2}秒",
            cartRingSnapshot.RingLength.Value,
            _trackTopology.CartSpacingMm,
            cartRingWarmupDuration);

        // 初始化小车到 CartLifecycleService
        for (int i = 0; i < cartRingSnapshot.RingLength.Value; i++)
        {
            var cartId = cartRingSnapshot.CartIds[i];
            _cartLifecycleService.InitializeCart(cartId, new CartIndex(i), DateTimeOffset.Now);
        }

        // 手动初始化 CartPositionTracker
        _cartPositionTracker.OnCartPassedOrigin(DateTimeOffset.Now);
        _logger.LogInformation("[CartRing] 小车位置跟踪器已初始化");

        // 步骤 3: 生成包裹并等待完成
        _logger.LogInformation("步骤 3/4: 开始生成包裹（目标: {ParcelCount} 个包裹）", _options.TargetParcelCount);
        await GenerateAndProcessParcelsAsync(cancellationToken);

        // 步骤 4: 收集统计信息
        _logger.LogInformation("步骤 4/4: 收集统计信息");
        var statistics = CalculateStatistics(startTime, stopwatch.Elapsed);

        var cartRingInfo = new CartRingInfo
        {
            Length = cartRingSnapshot.RingLength.Value,
            ZeroCartId = (int)cartRingSnapshot.ZeroCartId.Value,
            ZeroIndex = cartRingSnapshot.ZeroIndex.Value,
            CartSpacingMm = _trackTopology.CartSpacingMm,
            IsReady = true,
            WarmupDurationSeconds = cartRingWarmupDuration
        };

        var mainDriveInfo = new MainDriveInfo
        {
            TargetSpeedMmps = _options.MainLineSpeedMmps,
            AverageSpeedMmps = _speedProvider.CurrentMmps,
            SpeedStdDevMmps = 0m,
            MinSpeedMmps = _speedProvider.CurrentMmps,
            MaxSpeedMmps = _speedProvider.CurrentMmps
        };

        var sortingConfigInfo = new SortingConfigInfo
        {
            Scenario = "long-run-load-test",
            SortingMode = "Random",
            AvailableChutes = _options.ChuteCount - 1,
            ForceEjectChuteId = _options.ExceptionChuteId
        };

        var parcelDetails = CollectParcelDetails();

        stopwatch.Stop();

        var report = new SimulationReport
        {
            Statistics = statistics,
            CartRing = cartRingInfo,
            MainDrive = mainDriveInfo,
            SortingConfig = sortingConfigInfo,
            ParcelDetails = parcelDetails
        };

        _logger.LogInformation("[Simulation] 仿真完成，耗时: {Duration:F2} 秒", stopwatch.Elapsed.TotalSeconds);

        return report;
    }

    private async Task WaitForMainLineStableAsync(CancellationToken cancellationToken)
    {
        const int maxWaitSeconds = 10;
        var timeout = DateTime.Now.AddSeconds(maxWaitSeconds);

        while (DateTime.Now < timeout && !cancellationToken.IsCancellationRequested)
        {
            if (_mainLineControl.IsRunning && _speedProvider.IsSpeedStable)
            {
                _logger.LogInformation("主线已启动并稳定，当前速度: {Speed:F1} mm/s", _speedProvider.CurrentMmps);
                return;
            }

            await Task.Delay(100, cancellationToken);
        }

        _logger.LogWarning("主线未能在 {MaxWaitSeconds} 秒内稳定，继续执行", maxWaitSeconds);
    }

    private async Task WaitForCartRingReadyAsync(CancellationToken cancellationToken)
    {
        const int maxWaitSeconds = 90;
        var timeout = DateTime.Now.AddSeconds(maxWaitSeconds);
        var lastLogTime = DateTime.Now;

        _logger.LogInformation("等待小车环构建...");

        while (DateTime.Now < timeout && !cancellationToken.IsCancellationRequested)
        {
            var snapshot = _cartRingBuilder.CurrentSnapshot;
            if (snapshot != null && _cartPositionTracker.IsRingReady)
            {
                _logger.LogInformation(
                    "小车环已就绪 - 小车数量: {CartCount}, 零点车ID: {ZeroCartId}",
                    snapshot.RingLength.Value,
                    snapshot.ZeroCartId.Value);
                return;
            }

            if ((DateTime.Now - lastLogTime).TotalSeconds >= 5)
            {
                _logger.LogDebug(
                    "等待小车环就绪... (快照: {HasSnapshot}, 跟踪器就绪: {IsRingReady})",
                    snapshot != null,
                    _cartPositionTracker.IsRingReady);
                lastLogTime = DateTime.Now;
            }

            await Task.Delay(100, cancellationToken);
        }

        throw new TimeoutException($"小车环未能在 {maxWaitSeconds} 秒内完成构建并就绪");
    }

    private async Task GenerateAndProcessParcelsAsync(CancellationToken cancellationToken)
    {
        var intervalMs = _options.ParcelCreationIntervalMs;
        const int maxWaitSeconds = 600; // 最多等待10分钟
        var endTime = DateTime.Now.AddSeconds(maxWaitSeconds);

        _generatedCount = 0;

        // 持续生成包裹
        var generationTask = Task.Run(async () =>
        {
            while (_generatedCount < _options.TargetParcelCount && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // 生成包裹ID
                    var parcelId = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                    var parcelIdObj = new ParcelId(parcelId);

                    _logger.LogDebug("生成包裹 #{Counter} (ID: {ParcelId})", _generatedCount + 1, parcelId);

                    // 记录创建事件
                    var parcelLengthMm = GenerateRandomParcelLength();
                    _timelineRecorder.RecordEvent(parcelIdObj, "Created", 
                        $"入口传感器触发，包裹长度 {parcelLengthMm}mm");

                    // 触发入口传感器
                    _infeedSensor.SimulateParcelDetection();

                    _generatedCount++;

                    await Task.Delay(intervalMs, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "包裹生成过程中发生错误");
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                }
            }

            _logger.LogInformation("包裹生成完成，共生成 {Count} 个包裹", _generatedCount);
        }, cancellationToken);

        // 等待生成完成
        await generationTask;

        // 等待所有包裹处理完成
        _logger.LogInformation("等待所有包裹处理完成...");
        await WaitForAllParcelsCompletedAsync(endTime, cancellationToken);
    }

    private decimal GenerateRandomParcelLength()
    {
        var min = (double)_options.MinParcelLengthMm;
        var max = (double)_options.MaxParcelLengthMm;
        var length = min + _random.NextDouble() * (max - min);
        return (decimal)length;
    }

    private async Task WaitForAllParcelsCompletedAsync(DateTime endTime, CancellationToken cancellationToken)
    {
        const int checkIntervalMs = 500;
        var lastLogTime = DateTime.Now;

        while (DateTime.Now < endTime && !cancellationToken.IsCancellationRequested)
        {
            var allParcels = _parcelLifecycleService.GetAll();
            var terminatedStates = new[]
            {
                ParcelRouteState.Sorted,
                ParcelRouteState.ForceEjected,
                ParcelRouteState.Failed
            };

            var completedCount = allParcels.Count(p => terminatedStates.Contains(p.RouteState));

            if ((DateTime.Now - lastLogTime).TotalSeconds >= 2)
            {
                _logger.LogDebug(
                    "仿真进度 - 已生成: {GeneratedCount}/{TargetCount}, 已完成: {CompletedCount} ({CompletionPercentage:F1}%)",
                    _generatedCount,
                    _options.TargetParcelCount,
                    completedCount,
                    completedCount * 100.0 / _options.TargetParcelCount);
                lastLogTime = DateTime.Now;
            }

            // 判定完成条件：所有包裹都已生成且全部进入终态
            if (_generatedCount >= _options.TargetParcelCount && completedCount >= _options.TargetParcelCount)
            {
                _logger.LogInformation(
                    "仿真完成 - 目标包裹数: {TargetCount}, 已生成: {GeneratedCount}, 已完成: {CompletedCount}",
                    _options.TargetParcelCount,
                    _generatedCount,
                    completedCount);

                // 等待一小段时间确保所有操作完成
                await Task.Delay(1000, cancellationToken);
                return;
            }

            await Task.Delay(checkIntervalMs, cancellationToken);
        }

        _logger.LogWarning("等待包裹处理完成超时");
    }

    private SimulationStatistics CalculateStatistics(DateTime startTime, TimeSpan duration)
    {
        var allParcels = _parcelLifecycleService.GetAll();
        var totalParcels = allParcels.Count;

        var normalSorts = allParcels.Count(p => p.SortingOutcome == ParcelSortingOutcome.NormalSort);
        var forceEjects = allParcels.Count(p => p.SortingOutcome == ParcelSortingOutcome.ForceEject);
        var missorts = allParcels.Count(p => p.SortingOutcome == ParcelSortingOutcome.Missort);
        var unprocessed = allParcels.Count(p => p.SortingOutcome == ParcelSortingOutcome.Unprocessed || p.SortingOutcome == null);

        double successRate = totalParcels > 0 ? (double)normalSorts / totalParcels : 0.0;
        double forceEjectRate = totalParcels > 0 ? (double)forceEjects / totalParcels : 0.0;
        double missortRate = totalParcels > 0 ? (double)missorts / totalParcels : 0.0;
        double unprocessedRate = totalParcels > 0 ? (double)unprocessed / totalParcels : 0.0;

        return new SimulationStatistics
        {
            TotalParcels = totalParcels,
            SuccessfulSorts = normalSorts,
            ForceEjects = forceEjects,
            Missorts = missorts,
            Unprocessed = unprocessed,
            SuccessRate = successRate,
            ForceEjectRate = forceEjectRate,
            MissortRate = missortRate,
            UnprocessedRate = unprocessedRate,
            StartTime = startTime,
            EndTime = DateTime.Now,
            DurationSeconds = duration.TotalSeconds
        };
    }

    private List<ParcelDetail> CollectParcelDetails()
    {
        var allParcels = _parcelLifecycleService.GetAll();
        var details = new List<ParcelDetail>();

        foreach (var parcel in allParcels)
        {
            var outcome = parcel.SortingOutcome ?? ParcelSortingOutcome.Unprocessed;
            var isSuccess = outcome == ParcelSortingOutcome.NormalSort;
            var isForceEject = outcome == ParcelSortingOutcome.ForceEject;

            int? actualChuteId = parcel.ActualChuteId != null ? (int)parcel.ActualChuteId.Value.Value : null;

            string? failureReason = null;
            if (outcome == ParcelSortingOutcome.ForceEject)
            {
                failureReason = "强排";
            }
            else if (outcome == ParcelSortingOutcome.Missort)
            {
                failureReason = "误分";
            }
            else if (outcome == ParcelSortingOutcome.Unprocessed)
            {
                failureReason = "未处理";
            }

            details.Add(new ParcelDetail
            {
                ParcelId = $"PKG{parcel.ParcelId.Value:D6}",
                AssignedCartId = parcel.BoundCartId != null ? (int)parcel.BoundCartId.Value.Value : null,
                TargetChuteId = parcel.TargetChuteId != null ? (int)parcel.TargetChuteId.Value.Value : null,
                ActualChuteId = actualChuteId,
                IsSuccess = isSuccess,
                IsForceEject = isForceEject,
                FailureReason = failureReason
            });
        }

        return details;
    }
}

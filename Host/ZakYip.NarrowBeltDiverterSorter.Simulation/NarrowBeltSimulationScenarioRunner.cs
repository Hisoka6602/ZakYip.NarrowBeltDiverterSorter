using System.Diagnostics;
using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Configuration;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Carts;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Feeding;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.MainLine;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Parcels;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Sorting;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Tracking;
using ZakYip.NarrowBeltDiverterSorter.Simulation.Fakes;
using ZakYip.NarrowBeltDiverterSorter.Core.Enums;
using ZakYip.NarrowBeltDiverterSorter.Core.Enums.Configuration;

namespace ZakYip.NarrowBeltDiverterSorter.Simulation;

/// <summary>
/// 窄带分拣仿真场景运行器实现。
/// </summary>
public class NarrowBeltSimulationScenarioRunner : INarrowBeltSimulationScenarioRunner
{
    private readonly ILogger<NarrowBeltSimulationScenarioRunner> _logger;
    private readonly ICartRingBuilder _cartRingBuilder;
    private readonly ICartPositionTracker _cartPositionTracker;
    private readonly IParcelLifecycleService _parcelLifecycleService;
    private readonly IMainLineSpeedProvider _speedProvider;
    private readonly IMainLineControlService _mainLineControl;
    private readonly FakeInfeedSensorPort _infeedSensor;
    private readonly ParcelTimelineRecorder _timelineRecorder;
    private readonly Core.Domain.Topology.ITrackTopology _trackTopology;

    private int _generatedCount;
    private Random? _random;

    public NarrowBeltSimulationScenarioRunner(
        ILogger<NarrowBeltSimulationScenarioRunner> logger,
        ICartRingBuilder cartRingBuilder,
        ICartPositionTracker cartPositionTracker,
        IParcelLifecycleService parcelLifecycleService,
        IMainLineSpeedProvider speedProvider,
        IMainLineControlService mainLineControl,
        FakeInfeedSensorPort infeedSensor,
        ParcelTimelineRecorder timelineRecorder,
        Core.Domain.Topology.ITrackTopology trackTopology)
    {
        _logger = logger;
        _cartRingBuilder = cartRingBuilder;
        _cartPositionTracker = cartPositionTracker;
        _parcelLifecycleService = parcelLifecycleService;
        _speedProvider = speedProvider;
        _mainLineControl = mainLineControl;
        _infeedSensor = infeedSensor;
        _timelineRecorder = timelineRecorder;
        _trackTopology = trackTopology;
    }

    /// <inheritdoc/>
    public async Task<SimulationReport> RunAsync(
        NarrowBeltSimulationOptions simulationOptions,
        ChuteLayoutProfile chuteLayout,
        TargetChuteAssignmentProfile assignmentProfile,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.Now;
        var stopwatch = Stopwatch.StartNew();

        // 初始化随机数生成器
        _random = simulationOptions.RandomSeed.HasValue 
            ? new Random(simulationOptions.RandomSeed.Value)
            : new Random();

        _logger.LogInformation(
            "开始窄带分拣仿真 - 包裹数: {ParcelCount}, 格口数: {ChuteCount}, 创建间隔: {IntervalMs}ms",
            simulationOptions.TotalParcels,
            chuteLayout.ChuteCount,
            simulationOptions.TimeBetweenParcelsMs);

        // 步骤 1: 等待主线控制启动并稳定
        _logger.LogInformation("步骤 1/4: 等待主线控制启动并稳定");
        var mainLineWaitStart = DateTime.Now;
        await WaitForMainLineStableAsync(cancellationToken);
        var mainLineWaitDuration = (DateTime.Now - mainLineWaitStart).TotalSeconds;

        // 步骤 2: 等待小车环就绪
        _logger.LogInformation("步骤 2/4: 等待小车环构建完成");
        var cartRingWaitStart = DateTime.Now;
        await WaitForCartRingReadyAsync(cancellationToken);
        var cartRingWaitDuration = (DateTime.Now - cartRingWaitStart).TotalSeconds;

        // 步骤 3: 生成并处理包裹
        _logger.LogInformation("步骤 3/4: 开始生成和处理包裹");
        await GenerateAndProcessParcelsAsync(simulationOptions, chuteLayout, assignmentProfile, cancellationToken);

        // 步骤 4: 收集统计信息
        _logger.LogInformation("步骤 4/4: 收集仿真统计信息");
        stopwatch.Stop();

        var statistics = CalculateStatistics(startTime, stopwatch.Elapsed);
        var cartRingInfo = BuildCartRingInfo(cartRingWaitDuration);
        var mainDriveInfo = BuildMainDriveInfo();
        var sortingConfigInfo = BuildSortingConfigInfo(chuteLayout, assignmentProfile);
        var parcelDetails = CollectParcelDetails();

        var report = new SimulationReport
        {
            Statistics = statistics,
            CartRing = cartRingInfo,
            MainDrive = mainDriveInfo,
            SortingConfig = sortingConfigInfo,
            ParcelDetails = parcelDetails
        };

        _logger.LogInformation(
            "仿真完成 - 总包裹: {Total}, 成功: {Success} ({SuccessRate:P2}), 强排: {ForceEject}, 失败: {Failed}, 耗时: {Duration:F2}秒",
            statistics.TotalParcels,
            statistics.SuccessfulSorts,
            statistics.SuccessRate,
            statistics.ForceEjects,
            statistics.Missorts + statistics.Unprocessed,
            stopwatch.Elapsed.TotalSeconds);

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

            await Task.Delay(100, cancellationToken);
        }

        throw new TimeoutException($"小车环未能在 {maxWaitSeconds} 秒内完成构建并就绪");
    }

    private async Task GenerateAndProcessParcelsAsync(
        NarrowBeltSimulationOptions simulationOptions,
        ChuteLayoutProfile chuteLayout,
        TargetChuteAssignmentProfile assignmentProfile,
        CancellationToken cancellationToken)
    {
        var intervalMs = simulationOptions.TimeBetweenParcelsMs;
        var ttlSeconds = simulationOptions.ParcelTtlSeconds;
        var maxWaitSeconds = ttlSeconds + 60; // TTL + 额外缓冲时间
        var endTime = DateTime.Now.AddSeconds(maxWaitSeconds);

        _generatedCount = 0;

        // 初始化目标格口分配器
        var chuteAssigner = CreateChuteAssigner(chuteLayout, assignmentProfile);

        // 持续生成包裹
        var generationTask = Task.Run(async () =>
        {
            while (_generatedCount < simulationOptions.TotalParcels && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // 生成包裹ID
                    var parcelId = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                    var parcelIdObj = new ParcelId(parcelId);

                    // 分配目标格口
                    var targetChuteId = chuteAssigner.GetNextChute();

                    _logger.LogDebug(
                        "生成包裹 #{Counter} (ID: {ParcelId}, 目标格口: {TargetChute})", 
                        _generatedCount + 1, 
                        parcelId, 
                        targetChuteId);

                    // 记录创建事件
                    var parcelLengthMm = GenerateRandomParcelLength(simulationOptions);
                    _timelineRecorder.RecordEvent(
                        parcelIdObj, 
                        "Created", 
                        $"入口传感器触发，包裹长度 {parcelLengthMm:F0}mm，目标格口 {targetChuteId}");

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
        await WaitForAllParcelsCompletedAsync(simulationOptions.TotalParcels, endTime, cancellationToken);
    }

    private IChuteAssigner CreateChuteAssigner(ChuteLayoutProfile chuteLayout, TargetChuteAssignmentProfile assignmentProfile)
    {
        var exceptionChuteId = chuteLayout.GetExceptionChuteId();
        var availableChutes = Enumerable.Range(1, chuteLayout.ChuteCount)
            .Where(id => id != exceptionChuteId)
            .ToList();

        return assignmentProfile.Strategy switch
        {
            TargetChuteAssignmentStrategy.Random => new RandomChuteAssigner(
                availableChutes, 
                assignmentProfile.RandomSeed ?? _random!.Next()),
            TargetChuteAssignmentStrategy.RoundRobin => new RoundRobinChuteAssigner(availableChutes),
            TargetChuteAssignmentStrategy.Weighted => throw new NotImplementedException("Weighted策略待后续实现"),
            _ => throw new ArgumentException($"不支持的分配策略: {assignmentProfile.Strategy}")
        };
    }

    private decimal GenerateRandomParcelLength(NarrowBeltSimulationOptions options)
    {
        var min = (double)options.MinParcelLengthMm;
        var max = (double)options.MaxParcelLengthMm;
        var length = min + _random!.NextDouble() * (max - min);
        return (decimal)length;
    }

    private async Task WaitForAllParcelsCompletedAsync(int targetCount, DateTime endTime, CancellationToken cancellationToken)
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
                    targetCount,
                    completedCount,
                    completedCount * 100.0 / targetCount);
                lastLogTime = DateTime.Now;
            }

            // 判定完成条件：所有包裹都已生成且全部进入终态
            if (_generatedCount >= targetCount && completedCount >= targetCount)
            {
                _logger.LogInformation(
                    "仿真完成 - 目标包裹数: {TargetCount}, 已生成: {GeneratedCount}, 已完成: {CompletedCount}",
                    targetCount,
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

    private CartRingInfo BuildCartRingInfo(double warmupDurationSeconds)
    {
        var snapshot = _cartRingBuilder.CurrentSnapshot;
        if (snapshot == null)
        {
            return new CartRingInfo
            {
                Length = 0,
                ZeroCartId = 0,
                ZeroIndex = 0,
                CartSpacingMm = 0,
                IsReady = false,
                WarmupDurationSeconds = warmupDurationSeconds
            };
        }

        return new CartRingInfo
        {
            Length = snapshot.RingLength.Value,
            ZeroCartId = (int)snapshot.ZeroCartId.Value,
            ZeroIndex = snapshot.ZeroIndex.Value,
            CartSpacingMm = _trackTopology.CartSpacingMm,
            IsReady = _cartPositionTracker.IsRingReady,
            WarmupDurationSeconds = warmupDurationSeconds
        };
    }

    private MainDriveInfo BuildMainDriveInfo()
    {
        return new MainDriveInfo
        {
            TargetSpeedMmps = _speedProvider.CurrentMmps, // 仿真驱动当前速度即为目标速度
            AverageSpeedMmps = _speedProvider.CurrentMmps,
            SpeedStdDevMmps = 0m, // 仿真驱动无速度波动
            MinSpeedMmps = _speedProvider.CurrentMmps,
            MaxSpeedMmps = _speedProvider.CurrentMmps,
            IsFeedbackAvailable = true
        };
    }

    private SortingConfigInfo BuildSortingConfigInfo(ChuteLayoutProfile chuteLayout, TargetChuteAssignmentProfile assignmentProfile)
    {
        return new SortingConfigInfo
        {
            Scenario = "narrow-belt-configurable-scenario",
            SortingMode = assignmentProfile.Strategy.ToString(),
            FixedChuteId = null,
            AvailableChutes = chuteLayout.ChuteCount - 1, // 排除异常口
            ForceEjectChuteId = chuteLayout.GetExceptionChuteId()
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

            int? actualChuteId = parcel.ActualChuteId.HasValue ? (int)parcel.ActualChuteId.Value.Value : null;

            string? failureReason = outcome switch
            {
                ParcelSortingOutcome.ForceEject => $"强排 ({parcel.DiscardReason})",
                ParcelSortingOutcome.Missort => "误分",
                ParcelSortingOutcome.Unprocessed => "未处理",
                _ => null
            };

            details.Add(new ParcelDetail
            {
                ParcelId = $"PKG{parcel.ParcelId.Value:D6}",
                AssignedCartId = parcel.BoundCartId.HasValue ? (int)parcel.BoundCartId.Value.Value : null,
                TargetChuteId = parcel.TargetChuteId.HasValue ? (int)parcel.TargetChuteId.Value.Value : null,
                ActualChuteId = actualChuteId,
                IsSuccess = isSuccess,
                IsForceEject = isForceEject,
                FailureReason = failureReason
            });
        }

        return details;
    }
}

/// <summary>
/// 格口分配器接口。
/// </summary>
internal interface IChuteAssigner
{
    /// <summary>
    /// 获取下一个目标格口。
    /// </summary>
    int GetNextChute();
}

/// <summary>
/// 随机格口分配器。
/// </summary>
internal class RandomChuteAssigner : IChuteAssigner
{
    private readonly List<int> _availableChutes;
    private readonly Random _random;

    public RandomChuteAssigner(List<int> availableChutes, int seed)
    {
        _availableChutes = availableChutes;
        _random = new Random(seed);
    }

    public int GetNextChute()
    {
        var index = _random.Next(_availableChutes.Count);
        return _availableChutes[index];
    }
}

/// <summary>
/// 轮询格口分配器。
/// </summary>
internal class RoundRobinChuteAssigner : IChuteAssigner
{
    private readonly List<int> _availableChutes;
    private int _currentIndex;

    public RoundRobinChuteAssigner(List<int> availableChutes)
    {
        _availableChutes = availableChutes;
        _currentIndex = 0;
    }

    public int GetNextChute()
    {
        var chute = _availableChutes[_currentIndex];
        _currentIndex = (_currentIndex + 1) % _availableChutes.Count;
        return chute;
    }
}

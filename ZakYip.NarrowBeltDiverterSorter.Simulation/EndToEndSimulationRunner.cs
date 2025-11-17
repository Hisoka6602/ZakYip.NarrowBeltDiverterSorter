using System.Diagnostics;
using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Carts;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Feeding;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.MainLine;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Parcels;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Sorting;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Tracking;
using ZakYip.NarrowBeltDiverterSorter.Execution.MainLine;
using ZakYip.NarrowBeltDiverterSorter.Simulation.Fakes;
using ZakYip.NarrowBeltDiverterSorter.UpstreamContracts.Models;

namespace ZakYip.NarrowBeltDiverterSorter.Simulation;

/// <summary>
/// 端到端仿真运行器
/// 使用真实的领域服务链路，仅硬件接口使用 Fake 实现
/// </summary>
public class EndToEndSimulationRunner
{
    private readonly SimulationConfiguration _config;
    private readonly ILogger<EndToEndSimulationRunner> _logger;
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
    private readonly IMainLineDrive? _mainLineDrive;
    private readonly Core.Domain.Topology.ITrackTopology _trackTopology;

    private readonly List<SpeedSample> _speedSamples = new();

    public EndToEndSimulationRunner(
        SimulationConfiguration config,
        ILogger<EndToEndSimulationRunner> logger,
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
        IMainLineDrive? mainLineDrive = null)
    {
        _config = config;
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
        _mainLineDrive = mainLineDrive;
    }

    /// <summary>
    /// 运行端到端仿真
    /// </summary>
    public async Task<SimulationReport> RunAsync(int parcelCount, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("开始端到端仿真，包裹数量: {ParcelCount}", parcelCount);

        // 步骤 1: 等待主线控制启动并稳定
        _logger.LogInformation("步骤 1/4: 等待主线控制启动并稳定");
        await WaitForMainLineStableAsync(cancellationToken);

        // 步骤 2: 等待小车环自然构建完成（通过 CartMovementSimulator + OriginSensorMonitor）
        _logger.LogInformation("步骤 2/4: 等待小车环构建完成");
        var cartRingStartTime = DateTime.UtcNow;
        await WaitForCartRingReadyAsync(cancellationToken);
        var cartRingWarmupDuration = (DateTime.UtcNow - cartRingStartTime).TotalSeconds;

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
            _cartLifecycleService.InitializeCart(cartId, new CartIndex(i), DateTimeOffset.UtcNow);
        }
        
        // 手动初始化 CartPositionTracker - 在仿真环境中，我们知道零点车已经在原点位置
        // 这样可以避免等待下一个零点车通过原点（可能需要等待整个环的时间）
        _cartPositionTracker.OnCartPassedOrigin(DateTimeOffset.UtcNow);
        _logger.LogInformation("[CartRing] 小车位置跟踪器已初始化");


        // 步骤 3: 等待包裹生成和处理完成
        _logger.LogInformation("步骤 3/4: 等待包裹生成和处理（目标: {ParcelCount} 个包裹）", parcelCount);
        await WaitForSimulationCompletionAsync(parcelCount, cancellationToken);

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

        var mainDriveInfo = CalculateMainDriveInfo();

        // 收集分拣配置信息
        var strongEjectChuteId = _trackTopology.GetStrongEjectChuteId();
        var sortingConfigInfo = new SortingConfigInfo
        {
            Scenario = _config.Scenario,
            SortingMode = _config.SortingMode.ToString(),
            FixedChuteId = _config.SortingMode == SortingMode.FixedChute ? _config.FixedChuteId : null,
            AvailableChutes = _trackTopology.ChuteCount - 1, // 排除强排口
            ForceEjectChuteId = strongEjectChuteId.HasValue ? (int)strongEjectChuteId.Value.Value : 0
        };

        // 收集包裹详情
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

    /// <summary>
    /// 等待主线控制启动并速度稳定
    /// </summary>
    private async Task WaitForMainLineStableAsync(CancellationToken cancellationToken)
    {
        const int maxWaitSeconds = 10;
        var timeout = DateTime.UtcNow.AddSeconds(maxWaitSeconds);

        while (DateTime.UtcNow < timeout && !cancellationToken.IsCancellationRequested)
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

    /// <summary>
    /// 等待小车环构建完成（通过 CartMovementSimulator 和 OriginSensorMonitor 自然构建）
    /// </summary>
    private async Task WaitForCartRingReadyAsync(CancellationToken cancellationToken)
    {
        const int maxWaitSeconds = 90; // 增加到90秒，因为60辆小车需要更长时间完成一圈
        var timeout = DateTime.UtcNow.AddSeconds(maxWaitSeconds);
        var lastLogTime = DateTime.UtcNow;

        _logger.LogInformation("等待小车环构建...");

        while (DateTime.UtcNow < timeout && !cancellationToken.IsCancellationRequested)
        {
            // 检查小车环是否已构建完成并就绪
            var snapshot = _cartRingBuilder.CurrentSnapshot;
            if (snapshot != null && _cartPositionTracker.IsRingReady)
            {
                _logger.LogInformation(
                    "小车环已就绪 - 小车数量: {CartCount}, 零点车ID: {ZeroCartId}",
                    snapshot.RingLength.Value,
                    snapshot.ZeroCartId.Value);
                return;
            }

            // 每5秒输出一次等待日志
            if ((DateTime.UtcNow - lastLogTime).TotalSeconds >= 5)
            {
                _logger.LogDebug(
                    "等待小车环就绪... (快照: {HasSnapshot}, 跟踪器就绪: {IsRingReady})",
                    snapshot != null,
                    _cartPositionTracker.IsRingReady);
                lastLogTime = DateTime.UtcNow;
            }

            await Task.Delay(100, cancellationToken);
        }

        throw new TimeoutException($"小车环未能在 {maxWaitSeconds} 秒内完成构建并就绪");
    }

    /// <summary>
    /// 等待仿真完成：所有包裹都生成并进入终态
    /// </summary>
    private async Task WaitForSimulationCompletionAsync(int expectedParcelCount, CancellationToken cancellationToken)
    {
        const int samplingIntervalMs = 100; // 每100ms采样一次速度
        const int statusCheckIntervalMs = 500; // 每500ms检查一次包裹状态
        const int maxWaitSeconds = 300; // 最多等待5分钟作为兜底保护
        const double minCompletionRatio = 1.0; // 必须100%的包裹完成（正常落格或强排）

        var endTime = DateTime.UtcNow.AddSeconds(maxWaitSeconds);
        var lastStatusCheckTime = DateTime.UtcNow;
        var lastSpeedSampleTime = DateTime.UtcNow;
        
        _logger.LogInformation("开始等待包裹处理完成，目标包裹数: {ExpectedCount}", expectedParcelCount);

        while (DateTime.UtcNow < endTime && !cancellationToken.IsCancellationRequested)
        {
            // 定期采样主线速度
            if ((DateTime.UtcNow - lastSpeedSampleTime).TotalMilliseconds >= samplingIntervalMs)
            {
                lastSpeedSampleTime = DateTime.UtcNow;
                
                var currentSpeed = _speedProvider.CurrentMmps;
                if (currentSpeed > 0) // 忽略明显非法值
                {
                    _speedSamples.Add(new SpeedSample
                    {
                        Timestamp = DateTimeOffset.UtcNow,
                        SpeedMmps = currentSpeed
                    });
                }
            }

            // 定期检查仿真进度
            if ((DateTime.UtcNow - lastStatusCheckTime).TotalMilliseconds >= statusCheckIntervalMs)
            {
                lastStatusCheckTime = DateTime.UtcNow;

                // 从仓储/服务读取仿真进度
                var progress = GetSimulationProgress();
                
                _logger.LogDebug(
                    "仿真进度 - 已生成: {GeneratedCount}/{TargetCount}, 已完成: {CompletedCount} ({CompletionPercentage:F1}%)",
                    progress.GeneratedCount,
                    expectedParcelCount,
                    progress.CompletedCount,
                    progress.CompletedCount * 100.0 / expectedParcelCount);

                // 判定仿真完成条件：
                // 1. 所有包裹都已生成
                // 2. 至少95%的包裹进入终态（允许极少数包裹因异常未完成）
                if (progress.GeneratedCount >= expectedParcelCount)
                {
                    double completionRatio = (double)progress.CompletedCount / expectedParcelCount;
                    
                    if (completionRatio >= minCompletionRatio)
                    {
                        _logger.LogInformation(
                            "仿真完成 - 目标包裹数: {TargetCount}, 已生成: {GeneratedCount}, 已完成: {CompletedCount} ({CompletionRatio:P1})",
                            expectedParcelCount,
                            progress.GeneratedCount,
                            progress.CompletedCount,
                            completionRatio);
                        
                        // 等待一小段时间确保所有操作完成
                        await Task.Delay(500, cancellationToken);
                        return;
                    }
                    
                    // 如果完成率不足，继续等待但给出提示
                    _logger.LogDebug(
                        "等待剩余包裹完成处理 - 完成率: {CompletionRatio:P1} (目标: {MinRatio:P0})",
                        completionRatio,
                        minCompletionRatio);
                }
                else
                {
                    // 包裹还在生成中
                    _logger.LogDebug(
                        "包裹生成中 - 已生成: {GeneratedCount}/{TargetCount}",
                        progress.GeneratedCount,
                        expectedParcelCount);
                }
            }

            // 短暂延迟避免 CPU 占用过高
            await Task.Delay(50, cancellationToken);
        }

        // 兜底：超时后强制结束
        var finalProgress = GetSimulationProgress();
        
        _logger.LogWarning(
            "仿真等待超时（{MaxWaitSeconds} 秒），强制结束。当前进度: 已生成 {GeneratedCount}/{ExpectedCount}, 已完成 {CompletedCount} ({CompletionPercentage:F1}%), " +
            "正常落格 {SortedCount}, 强排 {ForceEjectedCount}, 失败 {FailedCount}",
            maxWaitSeconds,
            finalProgress.GeneratedCount,
            expectedParcelCount,
            finalProgress.CompletedCount,
            finalProgress.CompletedCount * 100.0 / expectedParcelCount,
            finalProgress.SortedCount,
            finalProgress.ForceEjectedCount,
            finalProgress.FailedCount);
    }

    /// <summary>
    /// 获取仿真进度（从仓储/服务读取）
    /// </summary>
    private SimulationProgress GetSimulationProgress()
    {
        var allParcels = _parcelLifecycleService.GetAll();
        
        // 终态定义：已分拣、强排、失败
        var terminatedStates = new[]
        {
            ParcelRouteState.Sorted,
            ParcelRouteState.ForceEjected,
            ParcelRouteState.Failed
        };
        
        var sortedCount = allParcels.Count(p => p.RouteState == ParcelRouteState.Sorted);
        var forceEjectedCount = allParcels.Count(p => p.RouteState == ParcelRouteState.ForceEjected);
        var failedCount = allParcels.Count(p => p.RouteState == ParcelRouteState.Failed);
        var completedCount = sortedCount + forceEjectedCount + failedCount;
        
        return new SimulationProgress
        {
            GeneratedCount = allParcels.Count,
            CompletedCount = completedCount,
            SortedCount = sortedCount,
            ForceEjectedCount = forceEjectedCount,
            FailedCount = failedCount
        };
    }
    
    /// <summary>
    /// 仿真进度信息
    /// </summary>
    private class SimulationProgress
    {
        public int GeneratedCount { get; set; }
        public int CompletedCount { get; set; }
        public int SortedCount { get; set; }
        public int ForceEjectedCount { get; set; }
        public int FailedCount { get; set; }
    }

    /// <summary>
    /// 从领域服务收集包裹详情
    /// </summary>
    private List<ParcelDetail> CollectParcelDetails()
    {
        var allParcels = _parcelLifecycleService.GetAll();
        var details = new List<ParcelDetail>();

        foreach (var parcel in allParcels)
        {
            // 根据 SortingOutcome 判断包裹分拣结果
            var outcome = parcel.SortingOutcome ?? ParcelSortingOutcome.Unprocessed;
            var isSuccess = outcome == ParcelSortingOutcome.NormalSort;
            var isForceEject = outcome == ParcelSortingOutcome.ForceEject;
            
            // 确定实际格口ID
            int? actualChuteId = parcel.ActualChuteId != null ? (int)parcel.ActualChuteId.Value.Value : null;
            
            // 确定失败原因
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

    private SimulationStatistics CalculateStatistics(DateTime startTime, TimeSpan duration)
    {
        // 从领域服务获取真实数据
        var allParcels = _parcelLifecycleService.GetAll();
        var totalParcels = allParcels.Count;

        // 统计各类包裹，基于 SortingOutcome
        var normalSorts = allParcels.Count(p => p.SortingOutcome == ParcelSortingOutcome.NormalSort);
        var forceEjects = allParcels.Count(p => p.SortingOutcome == ParcelSortingOutcome.ForceEject);
        var missorts = allParcels.Count(p => p.SortingOutcome == ParcelSortingOutcome.Missort);
        var unprocessed = allParcels.Count(p => p.SortingOutcome == ParcelSortingOutcome.Unprocessed || p.SortingOutcome == null);

        // 计算比率
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
            EndTime = DateTime.UtcNow,
            DurationSeconds = duration.TotalSeconds
        };
    }

    private MainDriveInfo CalculateMainDriveInfo()
    {
        // 检查反馈是否可用（仅适用于 Rema 模式）
        bool? isFeedbackAvailable = null;
        if (_mainLineDrive is Execution.MainLine.Rema.RemaLm1000HMainLineDrive remaDrive)
        {
            isFeedbackAvailable = remaDrive.IsFeedbackAvailable;
            
            if (!isFeedbackAvailable.Value)
            {
                _logger.LogWarning("主线速度反馈不可用 (Rema 模式)，速度统计数据可能不准确");
            }
        }
        
        if (_speedSamples.Count == 0)
        {
            _logger.LogWarning("主线速度采样为空，速度统计数据不可用");
            
            return new MainDriveInfo
            {
                TargetSpeedMmps = (decimal)_config.MainLineSpeedMmPerSec,
                AverageSpeedMmps = 0,
                SpeedStdDevMmps = 0,
                MinSpeedMmps = 0,
                MaxSpeedMmps = 0,
                IsFeedbackAvailable = isFeedbackAvailable
            };
        }

        var speeds = _speedSamples.Select(s => s.SpeedMmps).ToList();
        var avgSpeed = speeds.Average();
        var variance = speeds.Select(s => Math.Pow((double)(s - avgSpeed), 2)).Average();
        var stdDev = (decimal)Math.Sqrt(variance);

        _logger.LogInformation(
            "主线速度统计 - 目标: {TargetSpeed:F1} mm/s, 平均: {AvgSpeed:F1} mm/s, 标准差: {StdDev:F2} mm/s, 采样数: {SampleCount}",
            _config.MainLineSpeedMmPerSec,
            avgSpeed,
            stdDev,
            _speedSamples.Count);

        return new MainDriveInfo
        {
            TargetSpeedMmps = (decimal)_config.MainLineSpeedMmPerSec,
            AverageSpeedMmps = avgSpeed,
            SpeedStdDevMmps = stdDev,
            MinSpeedMmps = speeds.Min(),
            MaxSpeedMmps = speeds.Max(),
            IsFeedbackAvailable = isFeedbackAvailable
        };
    }

    private class SpeedSample
    {
        public DateTimeOffset Timestamp { get; set; }
        public decimal SpeedMmps { get; set; }
    }
}

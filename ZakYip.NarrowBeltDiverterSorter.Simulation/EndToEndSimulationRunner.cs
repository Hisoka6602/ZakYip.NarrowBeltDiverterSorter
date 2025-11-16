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

    private readonly List<SpeedSample> _speedSamples = new();

    public EndToEndSimulationRunner(
        SimulationConfiguration config,
        ILogger<EndToEndSimulationRunner> logger,
        ICartRingBuilder cartRingBuilder,
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
        InfeedLayoutOptions infeedLayout)
    {
        _config = config;
        _logger = logger;
        _cartRingBuilder = cartRingBuilder;
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
    }

    /// <summary>
    /// 运行端到端仿真
    /// </summary>
    public async Task<SimulationReport> RunAsync(int parcelCount, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("开始端到端仿真，包裹数量: {ParcelCount}", parcelCount);

        // 步骤 1: 启动主线控制
        _logger.LogInformation("步骤 1/5: 启动主线控制");
        await _mainLineControl.StartAsync(cancellationToken);

        // 步骤 2: 构建小车环
        _logger.LogInformation("步骤 2/5: 构建小车环");
        BuildCartRing();

        var cartRingSnapshot = _cartRingBuilder.CurrentSnapshot;
        if (cartRingSnapshot == null)
        {
            throw new InvalidOperationException("小车环构建失败");
        }

        _logger.LogInformation("小车环构建完成，长度: {RingLength}", cartRingSnapshot.RingLength.Value);

        // 初始化小车到 CartLifecycleService
        for (int i = 0; i < cartRingSnapshot.RingLength.Value; i++)
        {
            var cartId = cartRingSnapshot.CartIds[i];
            _cartLifecycleService.InitializeCart(cartId, new CartIndex(i), DateTimeOffset.UtcNow);
        }

        // 步骤 3: 生成并处理包裹
        _logger.LogInformation("步骤 3/5: 生成和处理包裹");
        await GenerateAndProcessParcelsAsync(parcelCount, cancellationToken);

        // 步骤 4: 执行分拣计划
        _logger.LogInformation("步骤 4/5: 执行分拣计划");
        await ExecuteSortingPlansAsync(cancellationToken);

        // 步骤 5: 收集统计信息
        _logger.LogInformation("步骤 5/5: 收集统计信息");
        var statistics = CalculateStatistics(startTime, stopwatch.Elapsed);

        var cartRingInfo = new CartRingInfo
        {
            Length = cartRingSnapshot.RingLength.Value,
            ZeroCartId = (int)cartRingSnapshot.ZeroCartId.Value,
            ZeroIndex = cartRingSnapshot.ZeroIndex.Value,
            CartSpacingMm = _config.CartSpacingMm
        };

        var mainDriveInfo = CalculateMainDriveInfo();

        // 收集包裹详情
        var parcelDetails = CollectParcelDetails();

        stopwatch.Stop();

        var report = new SimulationReport
        {
            Statistics = statistics,
            CartRing = cartRingInfo,
            MainDrive = mainDriveInfo,
            ParcelDetails = parcelDetails
        };

        _logger.LogInformation("仿真完成，耗时: {Duration:F2} 秒", stopwatch.Elapsed.TotalSeconds);

        return report;
    }

    /// <summary>
    /// 构建小车环（通过模拟原点传感器事件）
    /// </summary>
    private void BuildCartRing()
    {
        var timestamp = DateTimeOffset.UtcNow;

        // 第一次：模拟零点小车通过（触发两个传感器）
        _originSensor.SimulateCartPassing(isCartZero: true);
        _cartRingBuilder.OnOriginSensorTriggered(isFirstSensor: true, isRisingEdge: true, timestamp);
        _cartRingBuilder.OnOriginSensorTriggered(isFirstSensor: false, isRisingEdge: true, timestamp.AddMilliseconds(10));
        _cartRingBuilder.OnOriginSensorTriggered(isFirstSensor: true, isRisingEdge: false, timestamp.AddMilliseconds(50));
        _cartRingBuilder.OnOriginSensorTriggered(isFirstSensor: false, isRisingEdge: false, timestamp.AddMilliseconds(60));

        // 模拟其他小车通过（仅触发第一个传感器）
        for (int i = 1; i < _config.NumberOfCarts; i++)
        {
            timestamp = timestamp.AddMilliseconds(100);
            _originSensor.SimulateCartPassing(isCartZero: false);
            _cartRingBuilder.OnOriginSensorTriggered(isFirstSensor: true, isRisingEdge: true, timestamp);
            _cartRingBuilder.OnOriginSensorTriggered(isFirstSensor: true, isRisingEdge: false, timestamp.AddMilliseconds(50));
        }

        // 第二次：零点小车通过完成环
        timestamp = timestamp.AddMilliseconds(100);
        _originSensor.SimulateCartPassing(isCartZero: true);
        _cartRingBuilder.OnOriginSensorTriggered(isFirstSensor: true, isRisingEdge: true, timestamp);
        _cartRingBuilder.OnOriginSensorTriggered(isFirstSensor: false, isRisingEdge: true, timestamp.AddMilliseconds(10));
        _cartRingBuilder.OnOriginSensorTriggered(isFirstSensor: true, isRisingEdge: false, timestamp.AddMilliseconds(50));
        _cartRingBuilder.OnOriginSensorTriggered(isFirstSensor: false, isRisingEdge: false, timestamp.AddMilliseconds(60));
    }

    /// <summary>
    /// 生成并处理包裹（通过入口事件触发）
    /// </summary>
    private async Task GenerateAndProcessParcelsAsync(int parcelCount, CancellationToken cancellationToken)
    {
        for (int i = 0; i < parcelCount; i++)
        {
            var parcelIdLong = (long)(i + 1);
            var barcode = $"PKG{parcelIdLong:D6}";
            var infeedTime = DateTimeOffset.UtcNow;

            // 1. 模拟入口传感器触发，创建包裹
            var parcelSnapshot = _parcelLifecycleService.CreateParcel(
                new ParcelId(parcelIdLong),
                barcode,
                infeedTime);

            _logger.LogDebug("包裹 {ParcelId} 在入口触发", parcelIdLong);

            // 2. 请求上游分配格口
            var routingRequest = new ParcelRoutingRequestDto { ParcelId = parcelIdLong };
            var routingResponse = await _upstreamClient.RequestChuteAsync(routingRequest, cancellationToken);

            if (routingResponse.IsSuccess)
            {
                // 3. 绑定格口到包裹
                _parcelLifecycleService.BindChuteId(new ParcelId(parcelIdLong), new ChuteId(routingResponse.ChuteId));
                _logger.LogDebug("包裹 {ParcelId} 分配到格口 {ChuteId}", parcelIdLong, routingResponse.ChuteId);

                // 4. 使用 ParcelLoadPlanner 预测应该落到哪个小车
                var predictedCartId = await _loadPlanner.PredictLoadedCartAsync(infeedTime, cancellationToken);

                if (predictedCartId != null)
                {
                    var loadedTime = DateTimeOffset.UtcNow;

                    // 5. 绑定包裹到小车
                    _parcelLifecycleService.BindCartId(new ParcelId(parcelIdLong), predictedCartId.Value, loadedTime);
                    _cartLifecycleService.LoadParcel(predictedCartId.Value, new ParcelId(parcelIdLong));

                    _logger.LogDebug("包裹 {ParcelId} 装载到小车 {CartId}", parcelIdLong, predictedCartId.Value.Value);

                    // 更新路由状态为已装载
                    _parcelLifecycleService.UpdateRouteState(new ParcelId(parcelIdLong), ParcelRouteState.Loaded);
                }
            }

            // 记录速度样本
            _speedSamples.Add(new SpeedSample
            {
                Timestamp = DateTimeOffset.UtcNow,
                SpeedMmps = _speedProvider.CurrentMmps
            });

            // 模拟包裹生成间隔
            if (i < parcelCount - 1)
            {
                await Task.Delay(TimeSpan.FromSeconds(_config.ParcelGenerationIntervalSeconds), cancellationToken);
            }
        }
    }

    /// <summary>
    /// 执行分拣计划（使用 ISortingPlanner）
    /// </summary>
    private async Task ExecuteSortingPlansAsync(CancellationToken cancellationToken)
    {
        // 获取所有包裹
        var allParcels = _parcelLifecycleService.GetAll();
        var loadedParcels = allParcels.Where(p => 
            p.RouteState == ParcelRouteState.Loaded && 
            p.TargetChuteId != null && 
            p.BoundCartId != null).ToList();

        _logger.LogInformation("开始执行分拣计划，已装载包裹数: {Count}", loadedParcels.Count);

        // 使用 SortingPlanner 生成吐件计划
        var now = DateTimeOffset.UtcNow;
        var horizon = TimeSpan.FromSeconds(10);
        var ejectPlans = _sortingPlanner.PlanEjects(now, horizon);

        _logger.LogInformation("生成吐件计划数: {Count}", ejectPlans.Count);

        // 执行吐件计划（模拟格口打开）
        foreach (var plan in ejectPlans)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            // 模拟格口打开
            var chuteConfig = _chuteConfigProvider.GetConfig(plan.ChuteId);
            var openDuration = chuteConfig?.MaxOpenDuration ?? TimeSpan.FromMilliseconds(300);
            await _chuteTransmitter.OpenWindowAsync(plan.ChuteId, openDuration, cancellationToken);

            // 标记包裹已分拣
            _parcelLifecycleService.MarkSorted(plan.ParcelId, DateTimeOffset.UtcNow);
            _parcelLifecycleService.UpdateRouteState(plan.ParcelId, ParcelRouteState.Sorted);

            // 卸载小车
            _cartLifecycleService.UnloadCart(plan.CartId, DateTimeOffset.UtcNow);

            // 报告分拣结果给上游
            var report = new SortingResultReportDto
            {
                ParcelId = plan.ParcelId.Value,
                ChuteId = (int)plan.ChuteId.Value,
                IsSuccess = true
            };
            await _upstreamClient.ReportSortingResultAsync(report, cancellationToken);

            _logger.LogDebug("包裹 {ParcelId} 在格口 {ChuteId} 完成分拣", plan.ParcelId.Value, plan.ChuteId.Value);

            await Task.Delay(10, cancellationToken); // 模拟少量延迟
        }

        // 处理未被计划的包裹（可能需要强排）
        var sortedParcelIds = ejectPlans.Select(p => p.ParcelId).ToHashSet();
        var unsortedParcels = loadedParcels.Where(p => !sortedParcelIds.Contains(p.ParcelId)).ToList();

        if (unsortedParcels.Any())
        {
            _logger.LogInformation("处理未被计划的包裹数（可能强排）: {Count}", unsortedParcels.Count);

            foreach (var parcel in unsortedParcels)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                // 使用强排口
                var forceEjectChute = new ChuteId(_config.ForceEjectChuteId);
                await _chuteTransmitter.OpenWindowAsync(forceEjectChute, TimeSpan.FromMilliseconds(300), cancellationToken);

                _parcelLifecycleService.MarkSorted(parcel.ParcelId, DateTimeOffset.UtcNow);
                _parcelLifecycleService.UpdateRouteState(parcel.ParcelId, ParcelRouteState.Sorted);

                if (parcel.BoundCartId != null)
                {
                    _cartLifecycleService.UnloadCart(parcel.BoundCartId.Value, DateTimeOffset.UtcNow);
                }

                var report = new SortingResultReportDto
                {
                    ParcelId = parcel.ParcelId.Value,
                    ChuteId = _config.ForceEjectChuteId,
                    IsSuccess = false,
                    FailureReason = "强排"
                };
                await _upstreamClient.ReportSortingResultAsync(report, cancellationToken);

                _logger.LogDebug("包裹 {ParcelId} 强排到格口 {ChuteId}", parcel.ParcelId.Value, _config.ForceEjectChuteId);

                await Task.Delay(10, cancellationToken);
            }
        }
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
            var chuteConfig = parcel.TargetChuteId != null 
                ? _chuteConfigProvider.GetConfig(parcel.TargetChuteId) 
                : null;

            var isForceEject = chuteConfig?.IsForceEject ?? false;
            var isSuccess = parcel.RouteState == ParcelRouteState.Sorted;
            
            // 如果分拣成功且目标格口存在，实际格口就是目标格口
            // 如果失败或强排，实际格口是强排口
            int? actualChuteId = null;
            if (parcel.SortedAt != null)
            {
                if (isForceEject || !isSuccess)
                {
                    actualChuteId = _config.ForceEjectChuteId;
                }
                else if (parcel.TargetChuteId != null)
                {
                    actualChuteId = (int)parcel.TargetChuteId.Value;
                }
            }

            details.Add(new ParcelDetail
            {
                ParcelId = $"PKG{parcel.ParcelId.Value:D6}",
                AssignedCartId = parcel.BoundCartId != null ? (int)parcel.BoundCartId.Value : null,
                TargetChuteId = parcel.TargetChuteId != null ? (int)parcel.TargetChuteId.Value : null,
                ActualChuteId = actualChuteId,
                IsSuccess = isSuccess,
                IsForceEject = isForceEject,
                FailureReason = !isSuccess && parcel.SortedAt != null ? "强排" : null
            });
        }

        return details;
    }

    private SimulationStatistics CalculateStatistics(DateTime startTime, TimeSpan duration)
    {
        // 从领域服务获取真实数据
        var allParcels = _parcelLifecycleService.GetAll();
        var totalParcels = allParcels.Count;

        // 统计各类包裹
        var forceEjects = 0;
        var successful = 0;
        var missorts = 0;

        foreach (var parcel in allParcels)
        {
            if (parcel.RouteState == ParcelRouteState.Sorted && parcel.TargetChuteId != null)
            {
                var chuteConfig = _chuteConfigProvider.GetConfig(parcel.TargetChuteId);
                if (chuteConfig?.IsForceEject == true)
                {
                    forceEjects++;
                }
                else
                {
                    successful++;
                }
            }
            else if (parcel.SortedAt != null)
            {
                // 已分拣但状态不对，可能是误分
                missorts++;
            }
        }

        return new SimulationStatistics
        {
            TotalParcels = totalParcels,
            SuccessfulSorts = successful,
            ForceEjects = forceEjects,
            Missorts = missorts,
            StartTime = startTime,
            EndTime = DateTime.UtcNow,
            DurationSeconds = duration.TotalSeconds
        };
    }

    private MainDriveInfo CalculateMainDriveInfo()
    {
        if (_speedSamples.Count == 0)
        {
            return new MainDriveInfo
            {
                TargetSpeedMmps = (decimal)_config.MainLineSpeedMmPerSec,
                AverageSpeedMmps = 0,
                SpeedStdDevMmps = 0,
                MinSpeedMmps = 0,
                MaxSpeedMmps = 0
            };
        }

        var speeds = _speedSamples.Select(s => s.SpeedMmps).ToList();
        var avgSpeed = speeds.Average();
        var variance = speeds.Select(s => Math.Pow((double)(s - avgSpeed), 2)).Average();
        var stdDev = (decimal)Math.Sqrt(variance);

        return new MainDriveInfo
        {
            TargetSpeedMmps = (decimal)_config.MainLineSpeedMmPerSec,
            AverageSpeedMmps = avgSpeed,
            SpeedStdDevMmps = stdDev,
            MinSpeedMmps = speeds.Min(),
            MaxSpeedMmps = speeds.Max()
        };
    }

    private class SpeedSample
    {
        public DateTimeOffset Timestamp { get; set; }
        public decimal SpeedMmps { get; set; }
    }
}

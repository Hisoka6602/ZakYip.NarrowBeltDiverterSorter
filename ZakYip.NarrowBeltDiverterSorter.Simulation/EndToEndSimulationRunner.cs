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
    private readonly IUpstreamSortingApiClient _upstreamClient;
    private readonly IChuteConfigProvider _chuteConfigProvider;
    private readonly FakeInfeedSensorPort _infeedSensor;
    private readonly FakeOriginSensorPort _originSensor;
    private readonly FakeChuteTransmitterPort _chuteTransmitter;
    private readonly FakeMainLineDrivePort _mainLineDrive;
    private readonly FakeMainLineFeedbackPort _mainLineFeedback;

    private readonly List<SpeedSample> _speedSamples = new();
    private readonly List<ParcelTrackingInfo> _parcelTracking = new();

    public EndToEndSimulationRunner(
        SimulationConfiguration config,
        ILogger<EndToEndSimulationRunner> logger,
        ICartRingBuilder cartRingBuilder,
        IParcelLifecycleService parcelLifecycleService,
        ICartLifecycleService cartLifecycleService,
        IParcelLoadPlanner loadPlanner,
        ISortingPlanner sortingPlanner,
        IMainLineSpeedProvider speedProvider,
        IUpstreamSortingApiClient upstreamClient,
        IChuteConfigProvider chuteConfigProvider,
        FakeInfeedSensorPort infeedSensor,
        FakeOriginSensorPort originSensor,
        FakeChuteTransmitterPort chuteTransmitter,
        FakeMainLineDrivePort mainLineDrive,
        FakeMainLineFeedbackPort mainLineFeedback)
    {
        _config = config;
        _logger = logger;
        _cartRingBuilder = cartRingBuilder;
        _parcelLifecycleService = parcelLifecycleService;
        _cartLifecycleService = cartLifecycleService;
        _loadPlanner = loadPlanner;
        _sortingPlanner = sortingPlanner;
        _speedProvider = speedProvider;
        _upstreamClient = upstreamClient;
        _chuteConfigProvider = chuteConfigProvider;
        _infeedSensor = infeedSensor;
        _originSensor = originSensor;
        _chuteTransmitter = chuteTransmitter;
        _mainLineDrive = mainLineDrive;
        _mainLineFeedback = mainLineFeedback;
    }

    /// <summary>
    /// 运行端到端仿真
    /// </summary>
    public async Task<SimulationReport> RunAsync(int parcelCount, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("开始端到端仿真，包裹数量: {ParcelCount}", parcelCount);

        // 1. 构建小车环
        _logger.LogInformation("步骤 1/5: 构建小车环");
        await BuildCartRingAsync(cancellationToken);

        var cartRingSnapshot = _cartRingBuilder.CurrentSnapshot;
        if (cartRingSnapshot == null)
        {
            throw new InvalidOperationException("小车环构建失败");
        }

        _logger.LogInformation("小车环构建完成，长度: {RingLength}", cartRingSnapshot.RingLength.Value);

        // 2. 生成并处理包裹
        _logger.LogInformation("步骤 2/5: 生成和处理包裹");
        await GenerateAndProcessParcelsAsync(parcelCount, cancellationToken);

        // 3. 收集统计信息
        _logger.LogInformation("步骤 3/5: 收集统计信息");
        var statistics = CalculateStatistics(startTime, stopwatch.Elapsed);

        // 4. 收集小车环信息
        _logger.LogInformation("步骤 4/5: 收集小车环信息");
        var cartRingInfo = new CartRingInfo
        {
            Length = cartRingSnapshot.RingLength.Value,
            ZeroCartId = cartRingSnapshot.ZeroCartId.Value,
            ZeroIndex = cartRingSnapshot.ZeroIndex.Value,
            CartSpacingMm = _config.CartSpacingMm
        };

        // 5. 收集主驱速度信息
        _logger.LogInformation("步骤 5/5: 收集主驱速度信息");
        var mainDriveInfo = CalculateMainDriveInfo();

        stopwatch.Stop();

        var report = new SimulationReport
        {
            Statistics = statistics,
            CartRing = cartRingInfo,
            MainDrive = mainDriveInfo,
            ParcelDetails = _parcelTracking.Select(p => new ParcelDetail
            {
                ParcelId = p.ParcelId,
                AssignedCartId = p.AssignedCartId,
                TargetChuteId = p.TargetChuteId,
                ActualChuteId = p.ActualChuteId,
                IsSuccess = p.IsSuccess,
                IsForceEject = p.IsForceEject,
                FailureReason = p.FailureReason
            }).ToList()
        };

        _logger.LogInformation("仿真完成，耗时: {Duration:F2} 秒", stopwatch.Elapsed.TotalSeconds);

        return report;
    }

    private async Task BuildCartRingAsync(CancellationToken cancellationToken)
    {
        // 模拟小车通过原点传感器来构建小车环
        var timestamp = DateTimeOffset.UtcNow;

        // 模拟零点小车通过（触发两个传感器）
        _originSensor.SimulateTrigger(true, true, timestamp);
        await Task.Delay(50, cancellationToken);
        _originSensor.SimulateTrigger(false, true, timestamp.AddMilliseconds(50));
        await Task.Delay(50, cancellationToken);
        _originSensor.SimulateTrigger(true, false, timestamp.AddMilliseconds(100));
        await Task.Delay(50, cancellationToken);
        _originSensor.SimulateTrigger(false, false, timestamp.AddMilliseconds(150));

        _cartRingBuilder.OnOriginSensorTriggered(true, true, timestamp);
        _cartRingBuilder.OnOriginSensorTriggered(false, true, timestamp.AddMilliseconds(50));
        _cartRingBuilder.OnOriginSensorTriggered(true, false, timestamp.AddMilliseconds(100));
        _cartRingBuilder.OnOriginSensorTriggered(false, false, timestamp.AddMilliseconds(150));

        // 模拟其他小车通过（仅触发第一个传感器）
        for (int i = 1; i < _config.NumberOfCarts; i++)
        {
            await Task.Delay(100, cancellationToken);
            timestamp = timestamp.AddMilliseconds(100);
            _originSensor.SimulateTrigger(true, true, timestamp);
            _cartRingBuilder.OnOriginSensorTriggered(true, true, timestamp);
            
            await Task.Delay(50, cancellationToken);
            timestamp = timestamp.AddMilliseconds(50);
            _originSensor.SimulateTrigger(true, false, timestamp);
            _cartRingBuilder.OnOriginSensorTriggered(true, false, timestamp);
        }

        // 再次模拟零点小车通过完成环
        await Task.Delay(100, cancellationToken);
        timestamp = timestamp.AddMilliseconds(100);
        _originSensor.SimulateTrigger(true, true, timestamp);
        _originSensor.SimulateTrigger(false, true, timestamp.AddMilliseconds(50));
        _cartRingBuilder.OnOriginSensorTriggered(true, true, timestamp);
        _cartRingBuilder.OnOriginSensorTriggered(false, true, timestamp.AddMilliseconds(50));
        
        await Task.Delay(50, cancellationToken);
        timestamp = timestamp.AddMilliseconds(50);
        _originSensor.SimulateTrigger(true, false, timestamp);
        _originSensor.SimulateTrigger(false, false, timestamp.AddMilliseconds(50));
        _cartRingBuilder.OnOriginSensorTriggered(true, false, timestamp);
        _cartRingBuilder.OnOriginSensorTriggered(false, false, timestamp.AddMilliseconds(50));
    }

    private async Task GenerateAndProcessParcelsAsync(int parcelCount, CancellationToken cancellationToken)
    {
        for (int i = 0; i < parcelCount; i++)
        {
            var parcelId = $"PKG{i + 1:D6}";
            var trackingInfo = new ParcelTrackingInfo { ParcelId = parcelId };
            _parcelTracking.Add(trackingInfo);

            // 1. 入口生成包裹
            _parcelLifecycleService.CreateParcel(new ParcelId(parcelId));

            // 2. 请求上游分配格口
            var routingRequest = new ParcelRoutingRequestDto { ParcelId = parcelId };
            var routingResponse = await _upstreamClient.RequestChuteAsync(routingRequest, cancellationToken);

            if (routingResponse.IsSuccess)
            {
                trackingInfo.TargetChuteId = routingResponse.ChuteId;
                _parcelLifecycleService.RouteParcel(new ParcelId(parcelId), new ChuteId(routingResponse.ChuteId));
            }

            // 3. 分配小车并装载
            var cartRing = _cartRingBuilder.CurrentSnapshot;
            if (cartRing != null)
            {
                var cartIndex = i % cartRing.RingLength.Value;
                var cartId = new CartId(cartIndex);
                trackingInfo.AssignedCartId = cartId.Value;

                _parcelLifecycleService.LoadParcel(new ParcelId(parcelId), cartId);
                _cartLifecycleService.LoadCart(cartId, new ParcelId(parcelId));
            }

            // 4. 规划分拣
            var parcelSnapshot = _parcelLifecycleService.GetSnapshot(new ParcelId(parcelId));
            if (parcelSnapshot?.TargetChuteId != null && parcelSnapshot.BoundCartId != null)
            {
                var plan = _sortingPlanner.PlanForParcel(
                    parcelSnapshot.ParcelId,
                    parcelSnapshot.TargetChuteId,
                    parcelSnapshot.BoundCartId);

                // 5. 执行分拣（简化处理）
                var chuteConfig = _chuteConfigProvider.GetConfig(parcelSnapshot.TargetChuteId);
                if (chuteConfig != null)
                {
                    if (chuteConfig.IsForceEject)
                    {
                        trackingInfo.IsForceEject = true;
                        trackingInfo.ActualChuteId = chuteConfig.ChuteId.Value;
                    }
                    else
                    {
                        trackingInfo.ActualChuteId = parcelSnapshot.TargetChuteId.Value;
                    }

                    trackingInfo.IsSuccess = true;
                    _parcelLifecycleService.CompleteSorting(parcelSnapshot.ParcelId, parcelSnapshot.TargetChuteId);
                }
            }

            // 记录速度样本
            var currentSpeed = _speedProvider.CurrentMmps;
            _speedSamples.Add(new SpeedSample
            {
                Timestamp = DateTimeOffset.UtcNow,
                SpeedMmps = currentSpeed
            });

            // 模拟包裹间隔
            if (i < parcelCount - 1)
            {
                await Task.Delay(TimeSpan.FromSeconds(_config.ParcelGenerationIntervalSeconds), cancellationToken);
            }
        }
    }

    private SimulationStatistics CalculateStatistics(DateTime startTime, TimeSpan duration)
    {
        var totalParcels = _parcelTracking.Count;
        var forceEjects = _parcelTracking.Count(p => p.IsForceEject);
        var successful = _parcelTracking.Count(p => p.IsSuccess && !p.IsForceEject);
        var missorts = _parcelTracking.Count(p => !p.IsSuccess);

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

    private class ParcelTrackingInfo
    {
        public string ParcelId { get; set; } = string.Empty;
        public int? AssignedCartId { get; set; }
        public int? TargetChuteId { get; set; }
        public int? ActualChuteId { get; set; }
        public bool IsSuccess { get; set; }
        public bool IsForceEject { get; set; }
        public string? FailureReason { get; set; }
    }
}

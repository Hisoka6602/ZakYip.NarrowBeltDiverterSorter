using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Carts;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Parcels;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Sorting;
using ZakYip.NarrowBeltDiverterSorter.Execution.Sorting;

namespace ZakYip.NarrowBeltDiverterSorter.Simulation;

/// <summary>
/// 包裹分拣模拟器
/// 模拟包裹在小车上的运动，当到达格口时间窗口时执行分拣动作
/// </summary>
public class ParcelSortingSimulator : BackgroundService
{
    private readonly ILogger<ParcelSortingSimulator> _logger;
    private readonly ISortingPlanner _sortingPlanner;
    private readonly IEjectPlanner _ejectPlanner;
    private readonly ICartLifecycleService _cartLifecycleService;
    private readonly IParcelLifecycleService _parcelLifecycleService;
    private readonly IChuteConfigProvider _chuteConfigProvider;
    private readonly IChuteTransmitterPort _chuteTransmitterPort;
    private readonly SimulationConfiguration _config;
    private readonly Dictionary<ParcelId, DivertPlan> _activePlans = new();
    private readonly Dictionary<ParcelId, DateTimeOffset> _parcelLoadedTimes = new();

    public ParcelSortingSimulator(
        ILogger<ParcelSortingSimulator> logger,
        ISortingPlanner sortingPlanner,
        IEjectPlanner ejectPlanner,
        ICartLifecycleService cartLifecycleService,
        IParcelLifecycleService parcelLifecycleService,
        IChuteConfigProvider chuteConfigProvider,
        IChuteTransmitterPort chuteTransmitterPort,
        SimulationConfiguration config)
    {
        _logger = logger;
        _sortingPlanner = sortingPlanner;
        _ejectPlanner = ejectPlanner;
        _cartLifecycleService = cartLifecycleService;
        _parcelLifecycleService = parcelLifecycleService;
        _chuteConfigProvider = chuteConfigProvider;
        _chuteTransmitterPort = chuteTransmitterPort;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("包裹分拣模拟器已启动");

        // Wait for system to be ready
        await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);

        var checkIntervalMs = 50; // Check every 50ms

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTimeOffset.UtcNow;

                // 1. Check for parcels that need divert plans
                await UpdateDivertPlansAsync(now);

                // 2. Check for parcels in valid time windows
                await ExecuteDivertActionsAsync(now);

                // 3. Check for TTL expired parcels
                await HandleExpiredParcelsAsync(now);

                await Task.Delay(checkIntervalMs, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "包裹分拣模拟循环发生异常");
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }

        _logger.LogInformation("包裹分拣模拟器已停止");
    }

    /// <summary>
    /// 更新分流计划：为处于 Sorting 状态的包裹创建分流计划
    /// </summary>
    private async Task UpdateDivertPlansAsync(DateTimeOffset now)
    {
        var allParcels = _parcelLifecycleService.GetAll();

        foreach (var parcel in allParcels.Where(p => p.RouteState == ParcelRouteState.Sorting))
        {
            // Skip if already has a plan
            if (_activePlans.ContainsKey(parcel.ParcelId))
            {
                continue;
            }

            // Track when parcel was loaded
            if (parcel.LoadedAt.HasValue && !_parcelLoadedTimes.ContainsKey(parcel.ParcelId))
            {
                _parcelLoadedTimes[parcel.ParcelId] = parcel.LoadedAt.Value;
            }

            // Create divert plan if parcel has target chute and bound cart
            if (parcel.TargetChuteId.HasValue && parcel.BoundCartId.HasValue)
            {
                var plan = _ejectPlanner.CalculateDivertPlan(
                    parcel.BoundCartId.Value,
                    parcel.TargetChuteId.Value,
                    now);

                if (plan != null)
                {
                    plan = plan with { ParcelId = parcel.ParcelId };
                    _activePlans[parcel.ParcelId] = plan;

                    _logger.LogDebug(
                        "创建分流计划 - 包裹 {ParcelId}, 小车 {CartId}, 格口 {ChuteId}, 窗口 {WindowStart:HH:mm:ss.fff} - {WindowEnd:HH:mm:ss.fff}",
                        parcel.ParcelId.Value,
                        plan.CartId.Value,
                        plan.ChuteId.Value,
                        plan.WindowStart,
                        plan.WindowEnd);
                }
            }
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// 执行分流动作：检查包裹是否在时间窗口内，如果是则执行分流
    /// </summary>
    private async Task ExecuteDivertActionsAsync(DateTimeOffset now)
    {
        var plansToRemove = new List<ParcelId>();

        foreach (var kvp in _activePlans)
        {
            var parcelId = kvp.Key;
            var plan = kvp.Value;

            // Check if current time is within the time window
            if (now >= plan.WindowStart && now <= plan.WindowEnd)
            {
                var parcel = _parcelLifecycleService.Get(parcelId);
                if (parcel != null && parcel.RouteState == ParcelRouteState.Sorting)
                {
                    // Execute divert action
                    await ExecuteDivertAsync(parcel, plan);
                    plansToRemove.Add(parcelId);
                }
            }
            else if (now > plan.WindowEnd)
            {
                // Window has passed, remove plan
                _logger.LogWarning(
                    "包裹 {ParcelId} 的时间窗口已过期，未能执行分流",
                    parcelId.Value);
                plansToRemove.Add(parcelId);
            }
        }

        // Clean up executed or expired plans
        foreach (var parcelId in plansToRemove)
        {
            _activePlans.Remove(parcelId);
        }
    }

    /// <summary>
    /// 执行分流动作
    /// </summary>
    private async Task ExecuteDivertAsync(ParcelSnapshot parcel, DivertPlan plan)
    {
        try
        {
            // Open chute window (simulation)
            await _chuteTransmitterPort.OpenWindowAsync(
                plan.ChuteId,
                plan.WindowEnd - plan.WindowStart,
                CancellationToken.None);

            // Check if this is the correct chute (normal sort vs missort)
            var isCorrectChute = parcel.TargetChuteId.HasValue &&
                                 parcel.TargetChuteId.Value.Value == plan.ChuteId.Value;

            if (isCorrectChute)
            {
                // Normal sort - parcel ejected to correct chute
                _parcelLifecycleService.MarkSorted(parcel.ParcelId, DateTimeOffset.UtcNow);
                _parcelLifecycleService.UpdateSortingOutcome(
                    parcel.ParcelId,
                    ParcelSortingOutcome.NormalSort,
                    plan.ChuteId);

                _logger.LogInformation(
                    "[正常落格] 包裹 {ParcelId} 成功落入格口 {ChuteId}",
                    parcel.ParcelId.Value,
                    plan.ChuteId.Value);
            }
            else
            {
                // Missort - parcel ejected to wrong chute
                _parcelLifecycleService.UpdateRouteState(parcel.ParcelId, ParcelRouteState.Failed);
                _parcelLifecycleService.UpdateSortingOutcome(
                    parcel.ParcelId,
                    ParcelSortingOutcome.Missort,
                    plan.ChuteId);

                _logger.LogWarning(
                    "[误分] 包裹 {ParcelId} 落入错误格口 {ActualChute}，目标格口 {TargetChute}",
                    parcel.ParcelId.Value,
                    plan.ChuteId.Value,
                    parcel.TargetChuteId?.Value ?? -1);
            }

            // Unload cart and unbind parcel
            if (parcel.BoundCartId.HasValue)
            {
                _cartLifecycleService.UnloadCart(parcel.BoundCartId.Value, DateTimeOffset.UtcNow);
            }
            _parcelLifecycleService.UnbindCartId(parcel.ParcelId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "执行分流动作失败 - 包裹: {ParcelId}, 格口: {ChuteId}",
                parcel.ParcelId.Value,
                plan.ChuteId.Value);

            _parcelLifecycleService.UpdateRouteState(parcel.ParcelId, ParcelRouteState.Failed);
            _parcelLifecycleService.UpdateSortingOutcome(
                parcel.ParcelId,
                ParcelSortingOutcome.Unprocessed);
        }
    }

    /// <summary>
    /// 处理超过 TTL 的包裹，强排到强排口
    /// </summary>
    private async Task HandleExpiredParcelsAsync(DateTimeOffset now)
    {
        var allParcels = _parcelLifecycleService.GetAll();

        foreach (var parcel in allParcels.Where(p => p.RouteState == ParcelRouteState.Sorting))
        {
            // Check if parcel has exceeded TTL
            if (!_parcelLoadedTimes.TryGetValue(parcel.ParcelId, out var loadedTime))
            {
                continue;
            }

            var timeOnCart = (now - loadedTime).TotalSeconds;
            if (timeOnCart > _config.ParcelTimeToLiveSeconds)
            {
                // Force eject to force eject chute
                await ForceEjectParcelAsync(parcel);
                _parcelLoadedTimes.Remove(parcel.ParcelId);
                _activePlans.Remove(parcel.ParcelId);
            }
        }
    }

    /// <summary>
    /// 强排包裹到强排口
    /// </summary>
    private async Task ForceEjectParcelAsync(ParcelSnapshot parcel)
    {
        try
        {
            var forceEjectChuteId = new ChuteId(_config.ForceEjectChuteId);

            // Open force eject chute window
            await _chuteTransmitterPort.OpenWindowAsync(
                forceEjectChuteId,
                TimeSpan.FromSeconds(1),
                CancellationToken.None);

            // Update parcel state
            _parcelLifecycleService.UpdateRouteState(parcel.ParcelId, ParcelRouteState.ForceEjected);
            _parcelLifecycleService.UpdateSortingOutcome(
                parcel.ParcelId,
                ParcelSortingOutcome.ForceEject,
                forceEjectChuteId);

            _logger.LogWarning(
                "[强排] 包裹 {ParcelId} 超过 TTL，强制排出到格口 {ChuteId}",
                parcel.ParcelId.Value,
                forceEjectChuteId.Value);

            // Unload cart and unbind parcel
            if (parcel.BoundCartId.HasValue)
            {
                _cartLifecycleService.UnloadCart(parcel.BoundCartId.Value, DateTimeOffset.UtcNow);
            }
            _parcelLifecycleService.UnbindCartId(parcel.ParcelId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "强排包裹失败 - 包裹: {ParcelId}",
                parcel.ParcelId.Value);

            _parcelLifecycleService.UpdateRouteState(parcel.ParcelId, ParcelRouteState.Failed);
            _parcelLifecycleService.UpdateSortingOutcome(
                parcel.ParcelId,
                ParcelSortingOutcome.Unprocessed);
        }
    }
}

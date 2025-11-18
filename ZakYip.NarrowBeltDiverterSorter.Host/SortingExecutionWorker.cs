using ZakYip.NarrowBeltDiverterSorter.Core.Configuration;
using Microsoft.Extensions.Options;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Carts;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Parcels;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Sorting;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Execution.MainLine;
using ZakYip.NarrowBeltDiverterSorter.UpstreamContracts.Models;

namespace ZakYip.NarrowBeltDiverterSorter.Host;

/// <summary>
/// 分拣执行工作器
/// 周期性规划并执行吐件动作
/// </summary>
public class SortingExecutionWorker : BackgroundService
{
    private readonly ILogger<SortingExecutionWorker> _logger;
    private readonly ISortingPlanner _sortingPlanner;
    private readonly IChuteTransmitterPort _chuteTransmitterPort;
    private readonly ICartLifecycleService _cartLifecycleService;
    private readonly IParcelLifecycleService _parcelLifecycleService;
    private readonly IParcelLifecycleTracker _lifecycleTracker;
    private readonly IUpstreamSortingApiClient _upstreamApiClient;
    private readonly IChuteConfigProvider _chuteConfigProvider;
    private readonly IMainLineDrive _mainLineDrive;
    private readonly SortingExecutionOptions _options;
    private readonly bool _enableBringupLogging;

    public SortingExecutionWorker(
        ILogger<SortingExecutionWorker> logger,
        ISortingPlanner sortingPlanner,
        IChuteTransmitterPort chuteTransmitterPort,
        ICartLifecycleService cartLifecycleService,
        IParcelLifecycleService parcelLifecycleService,
        IParcelLifecycleTracker lifecycleTracker,
        IUpstreamSortingApiClient upstreamApiClient,
        IChuteConfigProvider chuteConfigProvider,
        IMainLineDrive mainLineDrive,
        IOptions<SortingExecutionOptions> options,
        StartupModeConfiguration startupConfig)
    {
        _logger = logger;
        _sortingPlanner = sortingPlanner;
        _chuteTransmitterPort = chuteTransmitterPort;
        _cartLifecycleService = cartLifecycleService;
        _parcelLifecycleService = parcelLifecycleService;
        _lifecycleTracker = lifecycleTracker;
        _upstreamApiClient = upstreamApiClient;
        _chuteConfigProvider = chuteConfigProvider;
        _mainLineDrive = mainLineDrive;
        _options = options.Value;
        _enableBringupLogging = startupConfig.EnableBringupLogging && 
                                startupConfig.Mode >= StartupMode.BringupChutes;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("分拣执行工作器已启动");

        var executionPeriod = _options.ExecutionPeriod;
        var planningHorizon = _options.PlanningHorizon;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // 检查主线是否已就绪
                if (!_mainLineDrive.IsReady)
                {
                    _logger.LogWarning("主线未就绪，跳过本次分拣执行周期");
                    await Task.Delay(executionPeriod, stoppingToken);
                    continue;
                }
                
                var now = DateTimeOffset.UtcNow;

                // Plan ejects
                var ejectPlans = _sortingPlanner.PlanEjects(now, planningHorizon);

                if (ejectPlans.Count > 0)
                {
                    _logger.LogDebug("规划了 {Count} 个吐件计划", ejectPlans.Count);
                    
                    foreach (var plan in ejectPlans)
                    {
                        _logger.LogInformation(
                            "[吐件规划] 包裹 {ParcelId} 小车 {CartId} 目标格口 {ChuteId} 强排={IsForceEject}",
                            plan.ParcelId.Value,
                            plan.CartId.Value,
                            plan.ChuteId.Value,
                            plan.IsForceEject);
                    }
                }

                // Execute plans
                foreach (var plan in ejectPlans)
                {
                    await ExecuteEjectPlanAsync(plan, stoppingToken);
                }

                // Wait for next execution cycle
                await Task.Delay(executionPeriod, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Normal stop
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分拣执行循环发生异常");
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }

        _logger.LogInformation("分拣执行工作器已停止");
    }

    /// <summary>
    /// 执行单个吐件计划
    /// </summary>
    private async Task ExecuteEjectPlanAsync(EjectPlan plan, CancellationToken cancellationToken)
    {
        try
        {
            // Bring-up 模式：输出吐件计划执行信息
            if (_enableBringupLogging)
            {
                _logger.LogInformation(
                    "[吐件计划执行] ParcelId={ParcelId}, CartId={CartId}, 目标格口={TargetChute}, 当前格口={CurrentChute}, 是否强排={IsForceEject}",
                    plan.ParcelId.Value,
                    plan.CartId.Value,
                    plan.ChuteId.Value,
                    plan.ChuteId.Value, // 简化：使用格口ID作为当前格口
                    plan.IsForceEject ? "是" : "否");
            }
            
            // Open chute window
            await _chuteTransmitterPort.OpenWindowAsync(
                plan.ChuteId,
                plan.OpenDuration,
                cancellationToken);

            if (plan.IsForceEject)
            {
                // Force eject: clear cart and update parcel state
                _cartLifecycleService.UnloadCart(plan.CartId, DateTimeOffset.UtcNow);
                
                var parcel = _parcelLifecycleService.Get(plan.ParcelId);
                if (parcel != null)
                {
                    _parcelLifecycleService.UpdateRouteState(plan.ParcelId, ParcelRouteState.ForceEjected);
                    _parcelLifecycleService.UnbindCartId(plan.ParcelId);

                    _logger.LogWarning(
                        "[强排] 包裹 {ParcelId} 被强制排出到格口 {ChuteId}（小车 {CartId}）",
                        plan.ParcelId.Value,
                        plan.ChuteId.Value,
                        plan.CartId.Value);

                    // Report to upstream
                    await ReportSortingResultAsync(
                        plan.ParcelId,
                        plan.ChuteId,
                        isSuccess: false,
                        failureReason: "ForceEjected",
                        cancellationToken);

                    // 更新生命周期状态：已落入异常格口
                    _lifecycleTracker.UpdateStatus(
                        plan.ParcelId,
                        ParcelStatus.DivertedToException,
                        ParcelFailureReason.None,
                        $"包裹被强排到格口 {plan.ChuteId.Value}");
                }
            }
            else
            {
                // Normal eject: mark parcel as sorted
                _cartLifecycleService.UnloadCart(plan.CartId, DateTimeOffset.UtcNow);
                _parcelLifecycleService.MarkSorted(plan.ParcelId, DateTimeOffset.UtcNow);
                _parcelLifecycleService.UnbindCartId(plan.ParcelId);

                // 更新生命周期状态：已成功落入目标格口
                _lifecycleTracker.UpdateStatus(
                    plan.ParcelId,
                    ParcelStatus.DivertedToTarget,
                    ParcelFailureReason.None,
                    $"包裹成功落入目标格口 {plan.ChuteId.Value}");

                _logger.LogInformation(
                    "[落格完成] 包裹 {ParcelId} 已落入格口 {ChuteId}（小车 {CartId}）",
                    plan.ParcelId.Value,
                    plan.ChuteId.Value,
                    plan.CartId.Value);

                // Report to upstream
                await ReportSortingResultAsync(
                    plan.ParcelId,
                    plan.ChuteId,
                    isSuccess: true,
                    failureReason: null,
                    cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "执行吐件计划失败 - 包裹: {ParcelId}, 小车: {CartId}, 格口: {ChuteId}",
                plan.ParcelId.Value,
                plan.CartId.Value,
                plan.ChuteId.Value);

            // Update parcel state to failed
            _parcelLifecycleService.UpdateRouteState(plan.ParcelId, ParcelRouteState.Failed);

            // 更新生命周期状态：失败
            _lifecycleTracker.UpdateStatus(
                plan.ParcelId,
                ParcelStatus.Failed,
                ParcelFailureReason.DeviceFault,
                $"执行吐件计划失败: {ex.Message}");

            // Report failure to upstream
            await ReportSortingResultAsync(
                plan.ParcelId,
                plan.ChuteId,
                isSuccess: false,
                failureReason: ex.Message,
                cancellationToken);
        }
    }

    /// <summary>
    /// 上报分拣结果到上游
    /// </summary>
    private async Task ReportSortingResultAsync(
        ParcelId parcelId,
        ChuteId chuteId,
        bool isSuccess,
        string? failureReason,
        CancellationToken cancellationToken)
    {
        try
        {
            var report = new SortingResultReportDto
            {
                ParcelId = parcelId.Value,
                ChuteId = (int)chuteId.Value,
                IsSuccess = isSuccess,
                FailureReason = failureReason,
                ReportTime = DateTimeOffset.UtcNow
            };

            await _upstreamApiClient.ReportSortingResultAsync(report, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "上报分拣结果失败 - 包裹: {ParcelId}, 格口: {ChuteId}",
                parcelId.Value,
                chuteId.Value);
        }
    }
}

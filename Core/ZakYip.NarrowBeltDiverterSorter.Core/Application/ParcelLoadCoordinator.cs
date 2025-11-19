using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Feeding;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Parcels;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Application;

/// <summary>
/// 包裹装载协调器
/// 协调包裹创建和装载到小车的过程
/// </summary>
public class ParcelLoadCoordinator
{
    private readonly IParcelLoadPlanner _loadPlanner;
    private readonly IParcelLifecycleTracker? _lifecycleTracker;
    private readonly Dictionary<ParcelId, ParcelSnapshot> _parcelSnapshots = new();
    private Action<string>? _logAction;

    /// <summary>
    /// 包裹装载到小车事件（已废弃，请订阅 IEventBus）
    /// </summary>
    [Obsolete("请使用 IEventBus 订阅 Observability.Events.ParcelLoadedOnCartEventArgs，此事件将在未来版本中移除")]
    public event EventHandler<ParcelLoadedOnCartEventArgs>? ParcelLoadedOnCart;

    /// <summary>
    /// 创建包裹装载协调器
    /// </summary>
    /// <param name="loadPlanner">装载计划器</param>
    /// <param name="lifecycleTracker">生命周期追踪器（可选）</param>
    public ParcelLoadCoordinator(IParcelLoadPlanner loadPlanner, IParcelLifecycleTracker? lifecycleTracker = null)
    {
        _loadPlanner = loadPlanner ?? throw new ArgumentNullException(nameof(loadPlanner));
        _lifecycleTracker = lifecycleTracker;
    }

    /// <summary>
    /// 设置日志输出委托（可选）
    /// </summary>
    public void SetLogAction(Action<string> logAction)
    {
        _logAction = logAction;
    }

    /// <summary>
    /// 处理包裹创建事件
    /// </summary>
    /// <param name="sender">事件源</param>
    /// <param name="e">事件参数</param>
    public void HandleParcelCreatedFromInfeed(object? sender, ParcelCreatedFromInfeedEventArgs e)
    {
        OnParcelCreatedFromInfeed(sender, e);
    }

    /// <summary>
    /// 获取所有包裹快照
    /// </summary>
    public IReadOnlyDictionary<ParcelId, ParcelSnapshot> GetParcelSnapshots()
    {
        return _parcelSnapshots;
    }

    private async void OnParcelCreatedFromInfeed(object? sender, ParcelCreatedFromInfeedEventArgs e)
    {
        // 调用装载计划器预测小车
        var predictedCartId = await _loadPlanner.PredictLoadedCartAsync(e.InfeedTriggerTime, CancellationToken.None);

        if (predictedCartId == null)
        {
            _logAction?.Invoke($"[上车规划等待] 包裹 {e.ParcelId.Value} 无法预测目标小车 - 小车环尚未就绪，包裹保持等待状态");
            
            // 不要将包裹标记为失败，保持在等待状态
            // 创建等待状态的快照
            var waitingSnapshot = new ParcelSnapshot
            {
                ParcelId = e.ParcelId,
                RouteState = ParcelRouteState.WaitingForRouting,
                CreatedAt = e.InfeedTriggerTime,
                Status = ParcelStatus.Created,
                FailureReason = ParcelFailureReason.None
            };
            _parcelSnapshots[e.ParcelId] = waitingSnapshot;
            return;
        }

        var loadedTime = DateTimeOffset.UtcNow;

        _logAction?.Invoke($"[上车规划] 包裹 {e.ParcelId.Value} 预测上车小车 {predictedCartId.Value.Value}");

        // 创建装载的包裹快照
        var snapshot = new ParcelSnapshot
        {
            ParcelId = e.ParcelId,
            BoundCartId = predictedCartId.Value,
            PredictedCartId = predictedCartId.Value,
            RouteState = ParcelRouteState.WaitingForRouting,
            CreatedAt = e.InfeedTriggerTime,
            LoadedAt = loadedTime,
            Status = ParcelStatus.OnMainline,
            FailureReason = ParcelFailureReason.None
        };

        // 更新快照集合
        _parcelSnapshots[e.ParcelId] = snapshot;

        // 更新生命周期追踪器（如果可用）
        _lifecycleTracker?.UpdateStatus(
            e.ParcelId,
            ParcelStatus.OnMainline,
            ParcelFailureReason.None,
            $"包裹已上车，预测小车: {predictedCartId.Value.Value}");

        // 发布装载事件（已废弃）
        var eventArgs = new ParcelLoadedOnCartEventArgs
        {
            ParcelId = e.ParcelId,
            CartId = predictedCartId.Value,
            LoadedTime = loadedTime
        };

#pragma warning disable CS0618 // Type or member is obsolete
        ParcelLoadedOnCart?.Invoke(this, eventArgs);
#pragma warning restore CS0618
    }
}

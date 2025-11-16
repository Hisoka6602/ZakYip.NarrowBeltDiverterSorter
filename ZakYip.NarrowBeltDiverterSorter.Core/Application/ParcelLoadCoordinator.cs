using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Feeding;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Application;

/// <summary>
/// 包裹装载协调器
/// 协调包裹创建和装载到小车的过程
/// </summary>
public class ParcelLoadCoordinator
{
    private readonly IParcelLoadPlanner _loadPlanner;
    private readonly Dictionary<ParcelId, ParcelSnapshot> _parcelSnapshots = new();
    private Action<string>? _logAction;

    /// <summary>
    /// 包裹装载到小车事件
    /// </summary>
    public event EventHandler<ParcelLoadedOnCartEventArgs>? ParcelLoadedOnCart;

    /// <summary>
    /// 创建包裹装载协调器
    /// </summary>
    /// <param name="loadPlanner">装载计划器</param>
    public ParcelLoadCoordinator(IParcelLoadPlanner loadPlanner)
    {
        _loadPlanner = loadPlanner ?? throw new ArgumentNullException(nameof(loadPlanner));
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
        _logAction?.Invoke($"[上车规划] 开始为包裹 {e.ParcelId.Value} 规划目标小车");
        
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
                CreatedAt = e.InfeedTriggerTime
            };
            _parcelSnapshots[e.ParcelId] = waitingSnapshot;
            return;
        }

        var loadedTime = DateTimeOffset.UtcNow;

        _logAction?.Invoke($"[上车规划成功] 包裹 {e.ParcelId.Value} -> 小车 {predictedCartId.Value.Value}, 预计到达时间: {loadedTime:o}");

        // 创建装载的包裹快照
        var snapshot = new ParcelSnapshot
        {
            ParcelId = e.ParcelId,
            BoundCartId = predictedCartId.Value,
            RouteState = ParcelRouteState.WaitingForRouting,
            CreatedAt = e.InfeedTriggerTime,
            LoadedAt = loadedTime
        };

        // 更新快照集合
        _parcelSnapshots[e.ParcelId] = snapshot;

        // 发布装载事件
        ParcelLoadedOnCart?.Invoke(this, new ParcelLoadedOnCartEventArgs
        {
            ParcelId = e.ParcelId,
            CartId = predictedCartId.Value,
            LoadedTime = loadedTime
        });
        
        _logAction?.Invoke($"[上车完成] 包裹 {e.ParcelId.Value} 已标记为在小车 {predictedCartId.Value.Value} 上");
    }
}

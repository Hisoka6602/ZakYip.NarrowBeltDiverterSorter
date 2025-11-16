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
            // 无法预测小车，创建失败状态的快照
            var failedSnapshot = new ParcelSnapshot
            {
                ParcelId = e.ParcelId,
                RouteState = ParcelRouteState.Failed,
                CreatedAt = e.InfeedTriggerTime
            };
            _parcelSnapshots[e.ParcelId] = failedSnapshot;
            return;
        }

        var loadedTime = DateTimeOffset.UtcNow;

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
    }
}

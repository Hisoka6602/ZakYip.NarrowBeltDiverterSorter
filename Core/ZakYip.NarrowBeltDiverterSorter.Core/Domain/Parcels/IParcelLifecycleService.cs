namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.Parcels;

/// <summary>
/// 包裹生命周期服务接口
/// 管理包裹的创建、状态更新和查询
/// </summary>
public interface IParcelLifecycleService
{
    /// <summary>
    /// 创建新包裹（入口事件触发时）
    /// </summary>
    /// <param name="parcelId">包裹ID</param>
    /// <param name="barcode">条码</param>
    /// <param name="infeedTriggerTime">入口触发时间</param>
    /// <returns>创建的包裹快照</returns>
    ParcelSnapshot CreateParcel(ParcelId parcelId, string barcode, DateTimeOffset infeedTriggerTime);

    /// <summary>
    /// 创建新包裹并绑定小车号（入口事件触发时）
    /// </summary>
    /// <param name="parcelId">包裹ID</param>
    /// <param name="barcode">条码</param>
    /// <param name="infeedTriggerTime">入口触发时间</param>
    /// <param name="cartNumber">小车号（1 基索引）</param>
    /// <returns>创建的包裹快照</returns>
    ParcelSnapshot CreateParcelWithCartNumber(ParcelId parcelId, string barcode, DateTimeOffset infeedTriggerTime, int cartNumber);

    /// <summary>
    /// 绑定格口ID（上游路由结果）
    /// </summary>
    /// <param name="parcelId">包裹ID</param>
    /// <param name="chuteId">格口ID</param>
    void BindChuteId(ParcelId parcelId, ChuteId chuteId);

    /// <summary>
    /// 绑定小车ID
    /// </summary>
    /// <param name="parcelId">包裹ID</param>
    /// <param name="cartId">小车ID</param>
    /// <param name="loadedTime">装载时间</param>
    void BindCartId(ParcelId parcelId, CartId cartId, DateTimeOffset loadedTime);

    /// <summary>
    /// 解绑小车ID
    /// </summary>
    /// <param name="parcelId">包裹ID</param>
    void UnbindCartId(ParcelId parcelId);

    /// <summary>
    /// 更新包裹路由状态
    /// </summary>
    /// <param name="parcelId">包裹ID</param>
    /// <param name="newState">新状态</param>
    void UpdateRouteState(ParcelId parcelId, ParcelRouteState newState);

    /// <summary>
    /// 标记包裹已分拣
    /// </summary>
    /// <param name="parcelId">包裹ID</param>
    /// <param name="sortedTime">分拣时间</param>
    void MarkSorted(ParcelId parcelId, DateTimeOffset sortedTime);

    /// <summary>
    /// 更新包裹分拣结果
    /// </summary>
    /// <param name="parcelId">包裹ID</param>
    /// <param name="outcome">分拣结果</param>
    /// <param name="actualChuteId">实际格口ID（可选）</param>
    /// <param name="discardReason">丢弃原因（可选，仅当强排时有效）</param>
    void UpdateSortingOutcome(ParcelId parcelId, ParcelSortingOutcome outcome, ChuteId? actualChuteId = null, ParcelDiscardReason? discardReason = null);

    /// <summary>
    /// 获取包裹快照
    /// </summary>
    /// <param name="parcelId">包裹ID</param>
    /// <returns>包裹快照，如果不存在返回null</returns>
    ParcelSnapshot? Get(ParcelId parcelId);

    /// <summary>
    /// 获取所有包裹快照
    /// </summary>
    /// <returns>所有包裹快照列表</returns>
    IReadOnlyList<ParcelSnapshot> GetAll();
}

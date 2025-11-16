using System.Collections.Concurrent;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Parcels;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Application;

/// <summary>
/// 包裹生命周期服务实现
/// 使用内存存储管理包裹状态
/// </summary>
public class ParcelLifecycleService : IParcelLifecycleService
{
    private readonly ConcurrentDictionary<ParcelId, ParcelSnapshot> _parcels = new();

    /// <inheritdoc/>
    public ParcelSnapshot CreateParcel(ParcelId parcelId, string barcode, DateTimeOffset infeedTriggerTime)
    {
        var parcel = new ParcelSnapshot
        {
            ParcelId = parcelId,
            RouteState = ParcelRouteState.WaitingForRouting,
            CreatedAt = infeedTriggerTime
        };

        if (!_parcels.TryAdd(parcelId, parcel))
        {
            throw new InvalidOperationException($"包裹 {parcelId.Value} 已存在");
        }

        return parcel;
    }

    /// <inheritdoc/>
    public void BindChuteId(ParcelId parcelId, ChuteId chuteId)
    {
        _parcels.AddOrUpdate(
            parcelId,
            _ => throw new InvalidOperationException($"包裹 {parcelId.Value} 不存在"),
            (_, existingParcel) => existingParcel with
            {
                TargetChuteId = chuteId,
                RouteState = ParcelRouteState.Routed
            });
    }

    /// <inheritdoc/>
    public void BindCartId(ParcelId parcelId, CartId cartId, DateTimeOffset loadedTime)
    {
        _parcels.AddOrUpdate(
            parcelId,
            _ => throw new InvalidOperationException($"包裹 {parcelId.Value} 不存在"),
            (_, existingParcel) => existingParcel with
            {
                BoundCartId = cartId,
                LoadedAt = loadedTime,
                RouteState = ParcelRouteState.Sorting
            });
    }

    /// <inheritdoc/>
    public void UnbindCartId(ParcelId parcelId)
    {
        _parcels.AddOrUpdate(
            parcelId,
            _ => throw new InvalidOperationException($"包裹 {parcelId.Value} 不存在"),
            (_, existingParcel) => existingParcel with
            {
                BoundCartId = null
            });
    }

    /// <inheritdoc/>
    public void UpdateRouteState(ParcelId parcelId, ParcelRouteState newState)
    {
        _parcels.AddOrUpdate(
            parcelId,
            _ => throw new InvalidOperationException($"包裹 {parcelId.Value} 不存在"),
            (_, existingParcel) => existingParcel with
            {
                RouteState = newState
            });
    }

    /// <inheritdoc/>
    public void MarkSorted(ParcelId parcelId, DateTimeOffset sortedTime)
    {
        _parcels.AddOrUpdate(
            parcelId,
            _ => throw new InvalidOperationException($"包裹 {parcelId.Value} 不存在"),
            (_, existingParcel) => existingParcel with
            {
                RouteState = ParcelRouteState.Sorted,
                SortedAt = sortedTime
            });
    }

    /// <inheritdoc/>
    public void UpdateSortingOutcome(ParcelId parcelId, ParcelSortingOutcome outcome, ChuteId? actualChuteId = null, ParcelDiscardReason? discardReason = null)
    {
        _parcels.AddOrUpdate(
            parcelId,
            _ => throw new InvalidOperationException($"包裹 {parcelId.Value} 不存在"),
            (_, existingParcel) => existingParcel with
            {
                SortingOutcome = outcome,
                ActualChuteId = actualChuteId,
                DiscardReason = discardReason
            });
    }

    /// <inheritdoc/>
    public ParcelSnapshot? Get(ParcelId parcelId)
    {
        return _parcels.TryGetValue(parcelId, out var parcel) ? parcel : null;
    }

    /// <inheritdoc/>
    public IReadOnlyList<ParcelSnapshot> GetAll()
    {
        return _parcels.Values.ToList();
    }
}

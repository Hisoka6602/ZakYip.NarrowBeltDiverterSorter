using System.Collections.Concurrent;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Carts;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Application;

/// <summary>
/// 小车生命周期服务实现
/// 使用内存存储管理小车状态
/// </summary>
public class CartLifecycleService : ICartLifecycleService
{
    private readonly ConcurrentDictionary<CartId, CartSnapshot> _carts = new();

    /// <inheritdoc/>
    public void InitializeCart(CartId cartId, CartIndex cartIndex, DateTimeOffset initialTime)
    {
        var cart = new CartSnapshot
        {
            CartId = cartId,
            CartIndex = cartIndex,
            IsLoaded = false,
            CurrentParcelId = null,
            LastResetAt = initialTime
        };

        _carts.TryAdd(cartId, cart);
    }

    /// <inheritdoc/>
    public void LoadParcel(CartId cartId, ParcelId parcelId)
    {
        _carts.AddOrUpdate(
            cartId,
            _ => throw new InvalidOperationException($"小车 {cartId.Value} 不存在"),
            (_, existingCart) => existingCart with
            {
                IsLoaded = true,
                CurrentParcelId = parcelId
            });
    }

    /// <inheritdoc/>
    public void UnloadCart(CartId cartId, DateTimeOffset resetTime)
    {
        _carts.AddOrUpdate(
            cartId,
            _ => throw new InvalidOperationException($"小车 {cartId.Value} 不存在"),
            (_, existingCart) => existingCart with
            {
                IsLoaded = false,
                CurrentParcelId = null,
                LastResetAt = resetTime
            });
    }

    /// <inheritdoc/>
    public CartSnapshot? Get(CartId cartId)
    {
        return _carts.TryGetValue(cartId, out var cart) ? cart : null;
    }

    /// <inheritdoc/>
    public IReadOnlyList<CartSnapshot> GetAll()
    {
        return _carts.Values.ToList();
    }
}

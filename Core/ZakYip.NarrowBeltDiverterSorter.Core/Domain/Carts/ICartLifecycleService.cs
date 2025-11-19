namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.Carts;

/// <summary>
/// 小车生命周期服务接口
/// 管理小车的状态更新和查询
/// </summary>
public interface ICartLifecycleService
{
    /// <summary>
    /// 初始化小车（从CartRingSnapshot创建）
    /// </summary>
    /// <param name="cartId">小车ID</param>
    /// <param name="cartIndex">小车索引</param>
    /// <param name="initialTime">初始化时间</param>
    void InitializeCart(CartId cartId, CartIndex cartIndex, DateTimeOffset initialTime);

    /// <summary>
    /// 装载包裹到小车
    /// </summary>
    /// <param name="cartId">小车ID</param>
    /// <param name="parcelId">包裹ID</param>
    void LoadParcel(CartId cartId, ParcelId parcelId);

    /// <summary>
    /// 卸载小车上的包裹（复位）
    /// </summary>
    /// <param name="cartId">小车ID</param>
    /// <param name="resetTime">复位时间</param>
    void UnloadCart(CartId cartId, DateTimeOffset resetTime);

    /// <summary>
    /// 获取小车快照
    /// </summary>
    /// <param name="cartId">小车ID</param>
    /// <returns>小车快照，如果不存在返回null</returns>
    CartSnapshot? Get(CartId cartId);

    /// <summary>
    /// 获取所有小车快照
    /// </summary>
    /// <returns>所有小车快照列表</returns>
    IReadOnlyList<CartSnapshot> GetAll();
}

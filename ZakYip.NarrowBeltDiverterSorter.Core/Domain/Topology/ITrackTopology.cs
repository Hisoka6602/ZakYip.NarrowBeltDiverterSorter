namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.Topology;

/// <summary>
/// 轨道拓扑接口
/// 描述窄带分拣机的轨道几何结构
/// </summary>
public interface ITrackTopology
{
    /// <summary>
    /// 小车数量
    /// </summary>
    int CartCount { get; }

    /// <summary>
    /// 小车节距（mm）
    /// </summary>
    decimal CartSpacingMm { get; }

    /// <summary>
    /// 环总长（mm）
    /// </summary>
    decimal RingTotalLengthMm { get; }

    /// <summary>
    /// 格口数量
    /// </summary>
    int ChuteCount { get; }

    /// <summary>
    /// 入口落包点相对于原点的距离（mm）
    /// </summary>
    decimal InfeedDropPointOffsetMm { get; }

    /// <summary>
    /// 获取小车在环上的位置（mm）
    /// </summary>
    /// <param name="cartId">小车ID</param>
    /// <returns>小车位置（mm），如果小车ID无效返回null</returns>
    decimal? GetCartPosition(CartId cartId);

    /// <summary>
    /// 根据位置获取小车ID
    /// </summary>
    /// <param name="positionMm">位置（mm）</param>
    /// <returns>最接近该位置的小车ID，如果位置无效返回null</returns>
    CartId? GetCartIdByPosition(decimal positionMm);

    /// <summary>
    /// 获取格口中心位置（mm）
    /// </summary>
    /// <param name="chuteId">格口ID</param>
    /// <returns>格口中心位置（mm），如果格口ID无效返回null</returns>
    decimal? GetChutePosition(ChuteId chuteId);

    /// <summary>
    /// 获取强排口格口ID
    /// </summary>
    /// <returns>强排口格口ID，如果没有强排口返回null</returns>
    ChuteId? GetStrongEjectChuteId();

    /// <summary>
    /// 获取格口的小车偏移量（相对于原点的小车数量）
    /// </summary>
    /// <param name="chuteId">格口ID</param>
    /// <returns>小车偏移量，如果格口ID无效返回null</returns>
    int? GetChuteCartOffset(ChuteId chuteId);
}

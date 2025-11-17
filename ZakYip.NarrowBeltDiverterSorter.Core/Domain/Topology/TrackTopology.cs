namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.Topology;

/// <summary>
/// 轨道拓扑实现
/// 描述窄带分拣机的轨道几何结构
/// </summary>
public class TrackTopology : ITrackTopology
{
    private readonly Dictionary<long, ChutePositionConfig> _chutePositions;
    private readonly ChuteId? _strongEjectChuteId;

    /// <summary>
    /// 创建轨道拓扑实例
    /// </summary>
    /// <param name="options">拓扑配置选项</param>
    /// <exception cref="ArgumentNullException">当options为null时抛出</exception>
    /// <exception cref="ArgumentException">当配置无效时抛出</exception>
    public TrackTopology(TrackTopologyOptions options)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        if (options.CartCount <= 0)
            throw new ArgumentException("小车数量必须大于0", nameof(options));

        if (options.CartSpacingMm <= 0)
            throw new ArgumentException("小车节距必须大于0", nameof(options));

        CartCount = options.CartCount;
        CartSpacingMm = options.CartSpacingMm;
        RingTotalLengthMm = CartCount * CartSpacingMm;
        InfeedDropPointOffsetMm = options.InfeedDropPointOffsetMm;

        // 构建格口位置字典
        _chutePositions = new Dictionary<long, ChutePositionConfig>();
        foreach (var chutePos in options.ChutePositions)
        {
            _chutePositions[chutePos.ChuteId.Value] = chutePos;
        }

        ChuteCount = _chutePositions.Count;

        // 设置强排口
        if (options.ForceEjectChuteId.HasValue && options.ForceEjectChuteId.Value > 0)
        {
            _strongEjectChuteId = new ChuteId(options.ForceEjectChuteId.Value);
        }
        else
        {
            _strongEjectChuteId = null;
        }
    }

    /// <inheritdoc/>
    public int CartCount { get; }

    /// <inheritdoc/>
    public decimal CartSpacingMm { get; }

    /// <inheritdoc/>
    public decimal RingTotalLengthMm { get; }

    /// <inheritdoc/>
    public int ChuteCount { get; }

    /// <inheritdoc/>
    public decimal InfeedDropPointOffsetMm { get; }

    /// <inheritdoc/>
    public decimal? GetCartPosition(CartId cartId)
    {
        if (cartId.Value < 0 || cartId.Value >= CartCount)
            return null;

        // 小车位置 = 小车ID * 节距
        return cartId.Value * CartSpacingMm;
    }

    /// <inheritdoc/>
    public CartId? GetCartIdByPosition(decimal positionMm)
    {
        if (positionMm < 0 || CartSpacingMm <= 0)
            return null;

        // 归一化位置到环内
        var normalizedPosition = positionMm % RingTotalLengthMm;
        if (normalizedPosition < 0)
            normalizedPosition += RingTotalLengthMm;

        // 计算最近的小车ID
        var cartId = (long)Math.Round(normalizedPosition / CartSpacingMm) % CartCount;
        return new CartId(cartId);
    }

    /// <inheritdoc/>
    public decimal? GetChutePosition(ChuteId chuteId)
    {
        if (!_chutePositions.TryGetValue(chuteId.Value, out var config))
            return null;

        // 格口位置 = 小车偏移量 * 节距
        return config.CartOffsetFromOrigin * CartSpacingMm;
    }

    /// <inheritdoc/>
    public ChuteId? GetStrongEjectChuteId()
    {
        return _strongEjectChuteId;
    }

    /// <inheritdoc/>
    public int? GetChuteCartOffset(ChuteId chuteId)
    {
        if (!_chutePositions.TryGetValue(chuteId.Value, out var config))
            return null;

        return config.CartOffsetFromOrigin;
    }
}

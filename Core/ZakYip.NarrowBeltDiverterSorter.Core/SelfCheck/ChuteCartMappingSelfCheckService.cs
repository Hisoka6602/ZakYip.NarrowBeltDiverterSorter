namespace ZakYip.NarrowBeltDiverterSorter.Core.SelfCheck;

/// <summary>
/// 格口-小车映射自检服务实现
/// 基于几何+拓扑验证格口与小车的映射关系，不依赖具体硬件实现
/// </summary>
public sealed class ChuteCartMappingSelfCheckService : IChuteCartMappingSelfCheckService
{
    /// <inheritdoc/>
    public ChuteCartMappingSelfCheckResult Analyze(
        IReadOnlyList<ChutePassEventArgs> chutePassEvents,
        TrackTopologySnapshot topology,
        ChuteCartMappingSelfCheckOptions options)
    {
        if (chutePassEvents == null)
            throw new ArgumentNullException(nameof(chutePassEvents));
        if (topology == null)
            throw new ArgumentNullException(nameof(topology));
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        // 按格口分组事件
        var eventsByChuteId = chutePassEvents
            .GroupBy(e => e.ChuteId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var checkItems = new List<ChuteCartMappingCheckItem>();

        // 为每个格口计算期望的小车编号并验证
        for (int chuteId = 1; chuteId <= topology.ChuteCount; chuteId++)
        {
            // 计算该格口的理论小车编号
            // 格口位置 = 格口宽度 × (格口编号 - 1) (假设格口从1开始编号，连续排列)
            var chutePositionMm = topology.ChuteWidthMm * (chuteId - 1);

            // 在主线上的位置（主线长度 = 格口宽度 × 格口数量 / 2）
            // 格口对应的小车编号 = round(格口位置 / 小车节距) % 小车数量
            var expectedCartIndex = (int)Math.Round(chutePositionMm / topology.CartSpacingMm);
            var expectedCartId = expectedCartIndex % topology.CartCount;

            // 获取该格口的观测事件
            var observedCartIds = new List<int>();
            if (eventsByChuteId.TryGetValue(chuteId, out var events))
            {
                observedCartIds = events.Select(e => e.CartId).ToList();
            }

            // 验证所有观测值是否在容差范围内
            var isPassed = ValidateObservedCartIds(
                expectedCartId,
                observedCartIds,
                topology.CartCount,
                options.CartIdTolerance);

            checkItems.Add(new ChuteCartMappingCheckItem
            {
                ChuteId = chuteId,
                ExpectedCartId = expectedCartId,
                ObservedCartIds = observedCartIds,
                IsPassed = isPassed
            });
        }

        var isAllPassed = checkItems.All(item => item.IsPassed);

        return new ChuteCartMappingSelfCheckResult
        {
            ChuteCount = topology.ChuteCount,
            CartCount = topology.CartCount,
            ChuteItems = checkItems,
            IsAllPassed = isAllPassed
        };
    }

    /// <summary>
    /// 验证观测到的小车编号是否在容差范围内
    /// </summary>
    private bool ValidateObservedCartIds(
        int expectedCartId,
        IReadOnlyList<int> observedCartIds,
        int cartCount,
        int tolerance)
    {
        if (observedCartIds.Count == 0)
        {
            // 没有观测数据，认为未通过
            return false;
        }

        // 检查每个观测值是否在容差范围内
        foreach (var observedCartId in observedCartIds)
        {
            // 计算环形距离（考虑环形拓扑）
            var distance = CalculateRingDistance(expectedCartId, observedCartId, cartCount);

            if (distance > tolerance)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 计算环形拓扑中两个小车编号的最短距离
    /// </summary>
    private int CalculateRingDistance(int cartId1, int cartId2, int cartCount)
    {
        var diff = Math.Abs(cartId1 - cartId2);
        var wrapAroundDiff = cartCount - diff;
        return Math.Min(diff, wrapAroundDiff);
    }
}

using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Topology;

namespace ZakYip.NarrowBeltDiverterSorter.Simulation;

/// <summary>
/// 从仿真配置构建轨道拓扑的构建器
/// </summary>
public static class TrackTopologyBuilder
{
    /// <summary>
    /// 从仿真配置构建轨道拓扑
    /// </summary>
    /// <param name="config">仿真配置</param>
    /// <returns>轨道拓扑实例</returns>
    public static TrackTopology BuildFromSimulationConfig(SimulationConfiguration config)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));

        var options = new TrackTopologyOptions
        {
            CartCount = config.NumberOfCarts,
            CartSpacingMm = config.CartSpacingMm,
            ForceEjectChuteId = config.ForceEjectChuteId > 0 ? config.ForceEjectChuteId : null,
            InfeedDropPointOffsetMm = config.InfeedToDropDistanceMm,
            ChutePositions = new List<ChutePositionConfig>()
        };

        // 构建格口位置配置
        // 格口间距为5个小车位（格口在位置 5, 10, 15, 20, 25, 30, 35, 40, 45, 50）
        for (int i = 1; i <= config.NumberOfChutes; i++)
        {
            options.ChutePositions.Add(new ChutePositionConfig
            {
                ChuteId = new ChuteId(i),
                CartOffsetFromOrigin = i * 5
            });
        }

        return new TrackTopology(options);
    }

    /// <summary>
    /// 从轨道拓扑构建格口配置列表
    /// </summary>
    /// <param name="topology">轨道拓扑</param>
    /// <param name="forceEjectChuteId">强排口ID（可选）</param>
    /// <returns>格口配置列表</returns>
    public static List<ChuteConfig> BuildChuteConfigs(ITrackTopology topology, int? forceEjectChuteId = null)
    {
        if (topology == null)
            throw new ArgumentNullException(nameof(topology));

        var configs = new List<ChuteConfig>();

        // 遍历所有格口并创建配置
        for (int i = 1; i <= topology.ChuteCount; i++)
        {
            var chuteId = new ChuteId(i);
            var cartOffset = topology.GetChuteCartOffset(chuteId);

            if (cartOffset.HasValue)
            {
                configs.Add(new ChuteConfig
                {
                    ChuteId = chuteId,
                    IsEnabled = true,
                    IsForceEject = (i == forceEjectChuteId),
                    CartOffsetFromOrigin = cartOffset.Value,
                    MaxOpenDuration = TimeSpan.FromMilliseconds(300)
                });
            }
        }

        return configs;
    }
}

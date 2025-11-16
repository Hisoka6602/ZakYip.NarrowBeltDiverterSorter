using ZakYip.NarrowBeltDiverterSorter.Communication.Upstream;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Feeding;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.MainLine;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Sorting;

namespace ZakYip.NarrowBeltDiverterSorter.Infrastructure.Configuration;

/// <summary>
/// 窄带分拣机默认配置种子类
/// 提供用于仿真的默认配置
/// </summary>
public static class NarrowBeltDefaultConfigSeeder
{
    /// <summary>
    /// 创建默认主线控制选项
    /// </summary>
    public static MainLineControlOptions CreateDefaultMainLineOptions()
    {
        return new MainLineControlOptions
        {
            TargetSpeedMmps = 1000m,
            LoopPeriod = TimeSpan.FromMilliseconds(100),
            ProportionalGain = 1.0m,
            IntegralGain = 0.1m,
            DerivativeGain = 0.01m,
            StableDeadbandMmps = 50m,
            StableHold = TimeSpan.FromSeconds(2),
            MinOutputMmps = 0m,
            MaxOutputMmps = 5000m,
            IntegralLimit = 1000m
        };
    }

    /// <summary>
    /// 创建默认入口布局选项
    /// </summary>
    public static InfeedLayoutOptions CreateDefaultInfeedLayoutOptions()
    {
        return new InfeedLayoutOptions
        {
            InfeedToMainLineDistanceMm = 2000m,
            TimeToleranceMs = 50,
            CartOffsetCalibration = 0
        };
    }

    /// <summary>
    /// 创建默认格口配置集
    /// </summary>
    /// <param name="numberOfChutes">格口数量</param>
    /// <param name="forceEjectChuteId">强排口ID</param>
    public static ChuteConfigSet CreateDefaultChuteConfigs(int numberOfChutes = 10, int forceEjectChuteId = 10)
    {
        var configs = new List<ChuteConfig>();
        
        for (int i = 1; i <= numberOfChutes; i++)
        {
            configs.Add(new ChuteConfig
            {
                ChuteId = new ChuteId(i),
                IsEnabled = true,
                IsForceEject = (i == forceEjectChuteId),
                CartOffsetFromOrigin = i * 2,
                MaxOpenDuration = TimeSpan.FromMilliseconds(300)
            });
        }

        return new ChuteConfigSet
        {
            Configs = configs
        };
    }

    /// <summary>
    /// 创建默认上游连接选项
    /// </summary>
    /// <param name="useFakeUpstream">是否使用假上游（默认为 true）</param>
    public static UpstreamConnectionOptions CreateDefaultUpstreamOptions(bool useFakeUpstream = true)
    {
        return new UpstreamConnectionOptions
        {
            BaseUrl = useFakeUpstream ? "http://localhost:5000" : "http://upstream-api:8080",
            RequestTimeoutSeconds = 30,
            AuthToken = null
        };
    }
}

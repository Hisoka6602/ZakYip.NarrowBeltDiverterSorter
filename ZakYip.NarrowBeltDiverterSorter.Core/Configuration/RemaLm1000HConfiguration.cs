namespace ZakYip.NarrowBeltDiverterSorter.Core.Configuration;

/// <summary>
/// 雷马 LM1000H 主线驱动配置
/// 包含 PID 参数、扭矩限制、稳速条件等
/// </summary>
public sealed record RemaLm1000HConfiguration
{
    /// <summary>
    /// 控制循环周期，默认 60 毫秒
    /// </summary>
    public int LoopPeriodMs { get; init; } = 60;
    
    /// <summary>
    /// 最大扭矩（0-1000，对应 0-100% 额定电流），默认 1000
    /// </summary>
    public int TorqueMax { get; init; } = 1000;
    
    /// <summary>
    /// 越速保护时的最大扭矩，默认 650
    /// </summary>
    public int TorqueMaxWhenOverLimit { get; init; } = 650;
    
    /// <summary>
    /// 过流保护时的最大扭矩，默认 700
    /// </summary>
    public int TorqueMaxWhenOverCurrent { get; init; } = 700;
    
    /// <summary>
    /// 高负载时的最大扭矩，默认 800
    /// </summary>
    public int TorqueMaxUnderHighLoad { get; init; } = 800;
    
    /// <summary>
    /// 限速频率（Hz），默认 25.0
    /// </summary>
    public decimal LimitHz { get; init; } = 25.0m;
    
    /// <summary>
    /// 允许的越速留边（Hz），默认 0.35
    /// </summary>
    public decimal LimitOvershootHz { get; init; } = 0.35m;
    
    /// <summary>
    /// 稳态死区（mm/s），默认 20.0
    /// </summary>
    public decimal StableDeadbandMmps { get; init; } = 20.0m;
    
    /// <summary>
    /// 稳态保持时间（毫秒），默认 1000
    /// </summary>
    public int StableHoldMs { get; init; } = 1000;
    
    /// <summary>
    /// 不稳定阈值（mm/s），默认 80.0
    /// </summary>
    public decimal UnstableThresholdMmps { get; init; } = 80.0m;
    
    /// <summary>
    /// 不稳定保持时间（毫秒），默认 800
    /// </summary>
    public int UnstableHoldMs { get; init; } = 800;
    
    /// <summary>
    /// 近稳态微带（mm/s），默认 20.0
    /// </summary>
    public decimal MicroBandMmps { get; init; } = 20.0m;
    
    /// <summary>
    /// 每个循环的扭矩爬坡限制，默认 18
    /// </summary>
    public int TorqueSlewPerLoop { get; init; } = 18;
    
    /// <summary>
    /// PID 比例增益，默认 0.28
    /// </summary>
    public decimal PidKp { get; init; } = 0.28m;
    
    /// <summary>
    /// PID 积分增益，默认 0.028
    /// </summary>
    public decimal PidKi { get; init; } = 0.028m;
    
    /// <summary>
    /// PID 微分增益，默认 0.005
    /// </summary>
    public decimal PidKd { get; init; } = 0.005m;
    
    /// <summary>
    /// PID 积分限幅，默认 500.0
    /// </summary>
    public decimal PidIntegralClamp { get; init; } = 500.0m;
    
    /// <summary>
    /// 最小速度（mm/s），默认 0.0
    /// </summary>
    public decimal MinMmps { get; init; } = 0.0m;
    
    /// <summary>
    /// 最大速度（mm/s），默认 3000.0
    /// </summary>
    public decimal MaxMmps { get; init; } = 3000.0m;
    
    /// <summary>
    /// 标准速度（mm/s），默认 1500.0
    /// </summary>
    public decimal StandardSpeedMmps { get; init; } = 1500.0m;
    
    /// <summary>
    /// 额定电流缩放比例，默认 1.0
    /// </summary>
    public decimal RatedCurrentScale { get; init; } = 1.0m;
    
    /// <summary>
    /// 后备额定电流（A），默认 6.0
    /// </summary>
    public decimal FallbackRatedCurrentA { get; init; } = 6.0m;
    
    /// <summary>
    /// 电流限制比例，默认 1.10
    /// </summary>
    public decimal CurrentLimitRatio { get; init; } = 1.10m;
    
    /// <summary>
    /// 过流积分衰减系数，默认 0.6
    /// </summary>
    public decimal OverCurrentIntegralDecay { get; init; } = 0.6m;
    
    /// <summary>
    /// 高负载比例，默认 0.90
    /// </summary>
    public decimal HighLoadRatio { get; init; } = 0.90m;
    
    /// <summary>
    /// 高负载保持时间（秒），默认 10
    /// </summary>
    public int HighLoadHoldSeconds { get; init; } = 10;
    
    /// <summary>
    /// 低速带宽度（mm/s），默认 350.0
    /// </summary>
    public decimal LowSpeedBandMmps { get; init; } = 350.0m;
    
    /// <summary>
    /// 低速摩擦补偿命令，默认 68.0
    /// </summary>
    public decimal FrictionCmd { get; init; } = 68.0m;
    
    /// <summary>
    /// 低速 Ki 增益提升倍数，默认 2.0
    /// </summary>
    public decimal LowSpeedKiBoost { get; init; } = 2.0m;
    
    /// <summary>
    /// 启动移动命令下限，默认 80
    /// </summary>
    public int StartMoveCmdFloor { get; init; } = 80;
    
    /// <summary>
    /// 创建默认配置
    /// </summary>
    public static RemaLm1000HConfiguration CreateDefault()
    {
        return new RemaLm1000HConfiguration();
    }
}

namespace ZakYip.NarrowBeltDiverterSorter.Execution.Vendors.Rema;

/// <summary>
/// 雷马 LM1000H 变频驱动器配置参数
/// 参考：ZakYip.ParcelSorter 项目中的 Rema 驱动实现
/// </summary>
public sealed record class RemaLm1000HOptions
{
    // —— 控制环节拍 ————————————————————————————————————————————————
    /// <summary>
    /// 控制主环的周期（采样/下发间隔）。越短越灵敏，噪声和CPU占用也越高。
    /// 典型取值：60–80ms。想要"更顺滑"可略增；想要"更跟手"可略减。
    /// </summary>
    public required TimeSpan LoopPeriod { get; init; }

    // —— 输出限幅（P3.10 工程值，0..1000=0..100% 额定电流 P2.06）—————————————
    /// <summary>
    /// 扭矩工程值最大上限。注意硬件物理上限为1000（=100%额定），超过1000会被硬顶。
    /// 参考：LM1000H 说明书 P3.10 参数定义
    /// 调大：给更强驱动能力；调小：全局更"温柔"。通常≤1000。
    /// </summary>
    public required int TorqueMax { get; init; }

    /// <summary>
    /// "越速保护"生效时允许的最大扭矩上限。越小，刹得越狠；越大，更温和。
    /// 参考：LM1000H 说明书 P3.10 参数定义
    /// 和软降步进一起决定回落的"台阶感"。
    /// </summary>
    public required int TorqueMaxWhenOverLimit { get; init; }

    /// <summary>
    /// 过流硬保护时的扭矩上限。越小越安全，但可能影响爬坡能力。
    /// 参考：LM1000H 说明书 P3.10 参数定义
    /// </summary>
    public required int TorqueMaxWhenOverCurrent { get; init; }

    /// <summary>
    /// 持续高负载时的扭矩上限。用于"温控/保守运行"，避免长时间贴额定。
    /// 参考：LM1000H 说明书 P3.10 参数定义
    /// </summary>
    public required int TorqueMaxUnderHighLoad { get; init; }

    // —— 限速（Hz，对应 P0.07）———————————————————————————————————————
    /// <summary>
    /// 顶频（Hz）。用于初始化写 P0.07，同时作为前馈标尺的上限参考。
    /// 参考：LM1000H 说明书 P0.07 - 限速频率
    /// 调小：更安全不易超速；调大：上限更靠近电机本体极限。
    /// </summary>
    public required decimal LimitHz { get; init; }

    /// <summary>
    /// 允许的"越速留边"（Hz）。相当于观望带：超出目标这么多Hz以内先不猛刹（配合软降步进）。
    /// 调大：更少干预、更顺滑；调小：更紧更"贴线"。
    /// 典型：0.30~0.40（=±30~40mm/s）。
    /// </summary>
    public required decimal LimitOvershootHz { get; init; }

    // —— 稳速条件（速度域：mm/s）—————————————————————————————————————
    /// <summary>
    /// 认定"进入稳态"的速度误差带宽（±）。越小越严格，可能触发更频繁的近稳逻辑。
    /// </summary>
    public required decimal StableDeadbandMmps { get; init; }

    /// <summary>
    /// 稳态保持时长。只有在 deadband 内持续这么久才发"FirstStabilized"。
    /// 调大：更谨慎；调小：更快上报稳态。
    /// </summary>
    public required TimeSpan StableHold { get; init; }

    /// <summary>
    /// 认定"不稳定事件"的阈值（误差幅度）。达到阈值并超过 UnstableHold 才上报事件。
    /// </summary>
    public required decimal UnstableThresholdMmps { get; init; }

    /// <summary>
    /// 不稳定保持时间。与 UnstableThresholdMmps 配合决定告警的敏感度。
    /// </summary>
    public required TimeSpan UnstableHold { get; init; }

    /// <summary>
    /// "近稳态微带"（±mm/s）：进入该带后，控制会更"糯"：
    ///  1) 积分参与按比例减弱（减振）；
    ///  2) 追加极小的"基于测量变化率"的抑振项（dOnMeas）；
    ///  3) 上/下斜率更小、小步软降。
    /// 调大：更早进入"糯模式"，波动更小但响应更钝；调小：更灵。
    /// 典型：15~25mm/s。
    /// </summary>
    public required decimal MicroBandMmps { get; init; }

    /// <summary>
    /// 单个环节拍允许的最大 P3.10 变化量（counts）。统一控制"上/下坡速"的硬尺子。
    /// 参考：LM1000H 说明书 P3.10 参数定义
    /// 调小：更细腻、更不易掉速过猛；调大：更跟手、更容易产生波动。
    /// 典型：12~25。近稳态下会自动按比例更小（更"糯"）。
    /// </summary>
    public required int TorqueSlewPerLoop { get; init; }

    // —— PID 参数 ———————————————————————————————————————————————
    /// <summary>
    /// 速度域 PID 参数（位置式内部实现）。Kp 增大更果断，Ki 增大稳态误差更小但易超调，Kd 抑制快速变化。
    /// </summary>
    public required PidGains Pid { get; init; }

    /// <summary>
    /// 积分项硬夹紧。越小"历史包袱"越轻，越不易长时间冲在一个方向上。
    /// </summary>
    public required decimal PidIntegralClamp { get; init; }

    // —— 显示/继电器/串口（可选）——————————————————————————————————
    /// <summary>
    /// 面板显示位（P7.07）。
    /// 参考：LM1000H 说明书 P7.07 - LED 运行显示参数
    /// </summary>
    public int? PanelBits { get; init; }

    /// <summary>
    /// 继电器定义（可选）。当前驱动层不直接下发继电器寄存器，参数会被记录与日志化，便于上层或设备固件使用。
    /// 参考：LM1000H 说明书 P6.02 - 继电器定义
    /// </summary>
    public int? RelayDefine { get; init; }

    /// <summary>
    /// 串口波特率（可选）。当前驱动层不直接配置底层串口，参数会被记录与日志化，便于上层或设备固件使用。
    /// 参考：LM1000H 说明书 Pd.01 - 通讯波特率
    /// </summary>
    public int? SerialBaud { get; init; }

    /// <summary>
    /// 串口数据格式（可选）。当前驱动层不直接配置底层串口，参数会被记录与日志化，便于上层或设备固件使用。
    /// 参考：LM1000H 说明书 Pd.02 - 数据格式
    /// </summary>
    public int? SerialFormat { get; init; }

    // —— 速度范围 / 标准值（mm/s）———————————————————————————————————
    /// <summary>
    /// 目标速度下限。SetSpeedAsync 会将指令速度夹在 [Min, Max]。
    /// </summary>
    public required decimal MinMmps { get; init; }

    /// <summary>
    /// 目标速度上限。SetSpeedAsync 会将指令速度夹在 [Min, Max]。
    /// </summary>
    public required decimal MaxMmps { get; init; }

    /// <summary>
    /// "标准/默认目标速度"。Enable 后，如果上层没有立即下发目标，
    /// 驱动会自动把目标设到该值（仅当该值在 [Min,Max] 范围内）。
    /// </summary>
    public decimal? StandardSpeedMmps { get; init; }

    // —— 电流监测与换算 ——————————————————————————————————————————
    /// <summary>
    /// 读取 P2.06 额定电流时的缩放；若寄存器值即真实安培，则取 1.0。
    /// 参考：LM1000H 说明书 P2.06 - 电机额定电流
    /// </summary>
    public required decimal RatedCurrentScale { get; init; }

    /// <summary>
    /// 读取 P2.06 失败时的兜底额定电流（A）。
    /// 参考：LM1000H 说明书 P2.06 - 电机额定电流
    /// </summary>
    public required decimal FallbackRatedCurrentA { get; init; }

    /// <summary>
    /// 硬限流判据：实际电流超过（CurrentLimitRatio × 额定）即认为过流。
    /// 参考：LM1000H 说明书 P2.06 - 电机额定电流
    /// 调小：更保守；调大：更激进。
    /// </summary>
    public required decimal CurrentLimitRatio { get; init; }

    /// <summary>
    /// 过流时对积分的衰减系数（0..1，越小卸载越狠）。避免过流后还在"推"。
    /// </summary>
    public required decimal OverCurrentIntegralDecay { get; init; }

    /// <summary>
    /// 连续达到额定电流比例即认定为"高负载"（软保护入口），例如 0.9=90%。
    /// 参考：LM1000H 说明书 P2.06 - 电机额定电流
    /// </summary>
    public required decimal HighLoadRatio { get; init; }

    /// <summary>
    /// 连续高负载保持时长，超过后会切到 TorqueMaxUnderHighLoad 上限。
    /// </summary>
    public required TimeSpan HighLoadHold { get; init; }

    // —— 低速辅助 ————————————————————————————————————————————————
    /// <summary>
    /// 低速区（≤该值）才启用"静摩擦补偿+Ki增强"等辅助。建议 300~400mm/s。
    /// </summary>
    public required decimal LowSpeedBandMmps { get; init; }

    /// <summary>
    /// 静摩擦补偿（沿误差方向给定的小常量，帮助"松车"）。过冲大则减，打不动则加。
    /// </summary>
    public required decimal FrictionCmd { get; init; }

    /// <summary>
    /// 低速区 Ki 增益倍数。越大越能消除偏低滞留，但更容易产生轻微过冲。
    /// </summary>
    public required decimal LowSpeedKiBoost { get; init; }

    /// <summary>
    /// 起步保底命令（滤波速度很低且目标>0时，至少给到此命令）。应高于 FrictionCmd ~20%。
    /// </summary>
    public required int StartMoveCmdFloor { get; init; }
}

/// <summary>
/// PID 控制参数
/// </summary>
public sealed record class PidGains
{
    /// <summary>
    /// 比例增益 Kp
    /// </summary>
    public required decimal Kp { get; init; }

    /// <summary>
    /// 积分增益 Ki
    /// </summary>
    public required decimal Ki { get; init; }

    /// <summary>
    /// 微分增益 Kd
    /// </summary>
    public required decimal Kd { get; init; }

    public PidGains() { }

    public PidGains(decimal kp, decimal ki, decimal kd)
    {
        Kp = kp;
        Ki = ki;
        Kd = kd;
    }
}

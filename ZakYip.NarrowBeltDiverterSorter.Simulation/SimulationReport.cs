using System.Text.Json.Serialization;

namespace ZakYip.NarrowBeltDiverterSorter.Simulation;

/// <summary>
/// 仿真报告
/// </summary>
public record class SimulationReport
{
    /// <summary>
    /// 总体统计信息
    /// </summary>
    public required SimulationStatistics Statistics { get; init; }

    /// <summary>
    /// 小车环信息
    /// </summary>
    public required CartRingInfo CartRing { get; init; }

    /// <summary>
    /// 主驱速度信息
    /// </summary>
    public required MainDriveInfo MainDrive { get; init; }

    /// <summary>
    /// 分拣配置信息
    /// </summary>
    public required SortingConfigInfo SortingConfig { get; init; }

    /// <summary>
    /// 格口IO信息（可选）
    /// </summary>
    public ChuteIoInfo? ChuteIo { get; init; }

    /// <summary>
    /// 逐包裹详细信息（可选）
    /// </summary>
    public List<ParcelDetail>? ParcelDetails { get; init; }

    /// <summary>
    /// 小车环自检结果（可选）
    /// </summary>
    public CartRingSelfCheckInfo? CartRingSelfCheck { get; init; }
}

/// <summary>
/// 仿真统计信息
/// </summary>
public record class SimulationStatistics
{
    /// <summary>
    /// 总包裹数
    /// </summary>
    public int TotalParcels { get; init; }

    /// <summary>
    /// 正常落格数
    /// </summary>
    public int SuccessfulSorts { get; init; }

    /// <summary>
    /// 强排数
    /// </summary>
    public int ForceEjects { get; init; }

    /// <summary>
    /// 误分数
    /// </summary>
    public int Missorts { get; init; }

    /// <summary>
    /// 未处理数
    /// </summary>
    public int Unprocessed { get; init; }

    /// <summary>
    /// 正常落格率
    /// </summary>
    public double SuccessRate { get; init; }

    /// <summary>
    /// 强排率
    /// </summary>
    public double ForceEjectRate { get; init; }

    /// <summary>
    /// 误分率
    /// </summary>
    public double MissortRate { get; init; }

    /// <summary>
    /// 未处理率
    /// </summary>
    public double UnprocessedRate { get; init; }

    /// <summary>
    /// 仿真开始时间
    /// </summary>
    public DateTime StartTime { get; init; }

    /// <summary>
    /// 仿真结束时间
    /// </summary>
    public DateTime EndTime { get; init; }

    /// <summary>
    /// 仿真耗时（秒）
    /// </summary>
    public double DurationSeconds { get; init; }

    /// <summary>
    /// 按生命周期状态分布（新增）
    /// </summary>
    public Dictionary<string, int>? StatusDistribution { get; init; }

    /// <summary>
    /// 按失败原因分布（新增）
    /// </summary>
    public Dictionary<string, int>? FailureReasonDistribution { get; init; }
}

/// <summary>
/// 小车环信息
/// </summary>
public record class CartRingInfo
{
    /// <summary>
    /// 小车环长度（小车数量）
    /// </summary>
    public int Length { get; init; }

    /// <summary>
    /// 零点小车ID
    /// </summary>
    public int ZeroCartId { get; init; }

    /// <summary>
    /// 零点索引
    /// </summary>
    public int ZeroIndex { get; init; }

    /// <summary>
    /// 小车节距（mm）
    /// </summary>
    public decimal CartSpacingMm { get; init; }

    /// <summary>
    /// 小车环是否就绪
    /// </summary>
    public bool IsReady { get; init; }

    /// <summary>
    /// 小车环预热耗时（秒）
    /// </summary>
    public double WarmupDurationSeconds { get; init; }
}

/// <summary>
/// 主驱速度信息
/// </summary>
public record class MainDriveInfo
{
    /// <summary>
    /// 目标速度（mm/s）
    /// </summary>
    public decimal TargetSpeedMmps { get; init; }

    /// <summary>
    /// 平均速度（mm/s）
    /// </summary>
    public decimal AverageSpeedMmps { get; init; }

    /// <summary>
    /// 速度标准差（mm/s）
    /// </summary>
    public decimal SpeedStdDevMmps { get; init; }

    /// <summary>
    /// 最小速度（mm/s）
    /// </summary>
    public decimal MinSpeedMmps { get; init; }

    /// <summary>
    /// 最大速度（mm/s）
    /// </summary>
    public decimal MaxSpeedMmps { get; init; }
    
    /// <summary>
    /// 反馈是否可用（Rema 模式下，如果连续读取失败，则为 false）
    /// </summary>
    public bool? IsFeedbackAvailable { get; init; }
}

/// <summary>
/// 分拣配置信息
/// </summary>
public record class SortingConfigInfo
{
    /// <summary>
    /// 仿真场景（例如：e2e-report, e2e-speed-unstable）
    /// </summary>
    public string? Scenario { get; init; }

    /// <summary>
    /// 分拣模式
    /// </summary>
    public required string SortingMode { get; init; }

    /// <summary>
    /// 固定格口ID（仅在 FixedChute 模式下有效）
    /// </summary>
    public int? FixedChuteId { get; init; }

    /// <summary>
    /// 可用格口数量
    /// </summary>
    public int AvailableChutes { get; init; }

    /// <summary>
    /// 强排口ID
    /// </summary>
    public int ForceEjectChuteId { get; init; }
}

/// <summary>
/// 包裹详细信息
/// </summary>
public record class ParcelDetail
{
    /// <summary>
    /// 包裹ID
    /// </summary>
    public required string ParcelId { get; init; }

    /// <summary>
    /// 分配的小车ID
    /// </summary>
    public int? AssignedCartId { get; init; }

    /// <summary>
    /// 目标格口ID
    /// </summary>
    public int? TargetChuteId { get; init; }

    /// <summary>
    /// 实际格口ID
    /// </summary>
    public int? ActualChuteId { get; init; }

    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// 是否强排
    /// </summary>
    public bool IsForceEject { get; init; }

    /// <summary>
    /// 失败原因
    /// </summary>
    public string? FailureReason { get; init; }

    /// <summary>
    /// 包裹生命周期状态（新增）
    /// </summary>
    public string? Status { get; init; }

    /// <summary>
    /// 包裹失败原因枚举（新增）
    /// </summary>
    public string? FailureReasonEnum { get; init; }
}

/// <summary>
/// 格口IO信息
/// </summary>
public record class ChuteIoInfo
{
    /// <summary>
    /// 格口IO模式（例如：Simulation, ZhiQian32Relay）
    /// </summary>
    public string? Mode { get; init; }

    /// <summary>
    /// 已映射的格口数量
    /// </summary>
    public int MappedChuteCount { get; init; }

    /// <summary>
    /// 使用的IP节点数量
    /// </summary>
    public int NodeCount { get; init; }

    /// <summary>
    /// 节点详情列表
    /// </summary>
    public List<ChuteIoNodeInfo>? Nodes { get; init; }

    /// <summary>
    /// 仿真期间成功执行的开操作次数
    /// </summary>
    public int OpenActionCount { get; init; }

    /// <summary>
    /// 仿真期间成功执行的关操作次数
    /// </summary>
    public int CloseActionCount { get; init; }

    /// <summary>
    /// 是否在停止时执行了 CloseAll
    /// </summary>
    public bool CloseAllExecuted { get; init; }
}

/// <summary>
/// 格口IO节点信息
/// </summary>
public record class ChuteIoNodeInfo
{
    /// <summary>
    /// 节点键
    /// </summary>
    public required string NodeKey { get; init; }

    /// <summary>
    /// IP地址
    /// </summary>
    public string? IpAddress { get; init; }

    /// <summary>
    /// 端口
    /// </summary>
    public int? Port { get; init; }

    /// <summary>
    /// 该节点控制的格口ID列表
    /// </summary>
    public required List<long> ControlledChutes { get; init; }
}

/// <summary>
/// 小车环自检信息
/// </summary>
public record class CartRingSelfCheckInfo
{
    /// <summary>
    /// 配置的小车数量
    /// </summary>
    public int ExpectedCartCount { get; init; }

    /// <summary>
    /// 检测到的小车数量
    /// </summary>
    public int MeasuredCartCount { get; init; }

    /// <summary>
    /// 配置的节距（mm）
    /// </summary>
    public decimal ExpectedPitchMm { get; init; }

    /// <summary>
    /// 估算出的节距（mm）
    /// </summary>
    public decimal MeasuredPitchMm { get; init; }

    /// <summary>
    /// 小车数量是否匹配
    /// </summary>
    public bool IsCartCountMatched { get; init; }

    /// <summary>
    /// 节距是否在容忍范围内
    /// </summary>
    public bool IsPitchWithinTolerance { get; init; }

    /// <summary>
    /// 节距误差容忍百分比
    /// </summary>
    public double TolerancePercent { get; init; }
}

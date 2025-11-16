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
    /// 逐包裹详细信息（可选）
    /// </summary>
    public List<ParcelDetail>? ParcelDetails { get; init; }
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
}

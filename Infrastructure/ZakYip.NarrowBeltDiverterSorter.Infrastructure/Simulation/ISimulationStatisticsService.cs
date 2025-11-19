namespace ZakYip.NarrowBeltDiverterSorter.Infrastructure.Simulation;

/// <summary>
/// 仿真统计服务接口
/// </summary>
/// <remarks>
/// 用于跟踪和查询仿真过程中的统计数据
/// </remarks>
public interface ISimulationStatisticsService
{
    /// <summary>
    /// 开始新的仿真运行
    /// </summary>
    /// <param name="runId">运行标识符</param>
    void StartRun(string runId);

    /// <summary>
    /// 结束仿真运行
    /// </summary>
    /// <param name="runId">运行标识符</param>
    void EndRun(string runId);

    /// <summary>
    /// 记录包裹创建
    /// </summary>
    /// <param name="runId">运行标识符</param>
    /// <param name="parcelId">包裹ID</param>
    void RecordParcelCreated(string runId, long parcelId);

    /// <summary>
    /// 记录包裹分拣到目标格口
    /// </summary>
    /// <param name="runId">运行标识符</param>
    /// <param name="parcelId">包裹ID</param>
    /// <param name="targetChuteId">目标格口ID</param>
    /// <param name="actualChuteId">实际格口ID</param>
    void RecordParcelSorted(string runId, long parcelId, int targetChuteId, int actualChuteId);

    /// <summary>
    /// 记录包裹分拣到异常口
    /// </summary>
    /// <param name="runId">运行标识符</param>
    /// <param name="parcelId">包裹ID</param>
    void RecordParcelToErrorChute(string runId, long parcelId);

    /// <summary>
    /// 记录包裹超时
    /// </summary>
    /// <param name="runId">运行标识符</param>
    /// <param name="parcelId">包裹ID</param>
    void RecordParcelTimedOut(string runId, long parcelId);

    /// <summary>
    /// 获取仿真统计结果
    /// </summary>
    /// <param name="runId">运行标识符</param>
    /// <returns>统计结果，如果运行不存在则返回 null</returns>
    SimulationStatistics? GetStatistics(string runId);

    /// <summary>
    /// 获取当前活动的仿真运行ID
    /// </summary>
    /// <returns>活动的运行ID，如果没有则返回 null</returns>
    string? GetActiveRunId();
}

/// <summary>
/// 仿真统计数据
/// </summary>
public record SimulationStatistics
{
    /// <summary>
    /// 运行标识符
    /// </summary>
    public required string RunId { get; init; }

    /// <summary>
    /// 总包裹数
    /// </summary>
    public int TotalParcels { get; init; }

    /// <summary>
    /// 分拣到目标格口的包裹数
    /// </summary>
    public int SortedToTargetChutes { get; init; }

    /// <summary>
    /// 分拣到异常口的包裹数
    /// </summary>
    public int SortedToErrorChute { get; init; }

    /// <summary>
    /// 超时的包裹数
    /// </summary>
    public int TimedOutCount { get; init; }

    /// <summary>
    /// 错分的包裹数
    /// </summary>
    public int MisSortedCount { get; init; }

    /// <summary>
    /// 是否已完成
    /// </summary>
    public bool IsCompleted { get; init; }

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTimeOffset? StartTime { get; init; }

    /// <summary>
    /// 结束时间
    /// </summary>
    public DateTimeOffset? EndTime { get; init; }
}

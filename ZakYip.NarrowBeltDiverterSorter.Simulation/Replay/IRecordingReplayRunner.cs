using ZakYip.NarrowBeltDiverterSorter.Observability.Recording;

namespace ZakYip.NarrowBeltDiverterSorter.Simulation.Replay;

/// <summary>
/// 回放模式
/// </summary>
public enum ReplayMode
{
    /// <summary>
    /// 原速回放 - 保持原始事件间隔
    /// </summary>
    OriginalSpeed,

    /// <summary>
    /// 加速回放 - 将间隔除以加速倍数
    /// </summary>
    Accelerated,

    /// <summary>
    /// 固定间隔回放 - 使用固定的时间间隔
    /// </summary>
    FixedInterval
}

/// <summary>
/// 回放配置
/// </summary>
public record ReplayConfiguration
{
    /// <summary>
    /// 回放模式
    /// </summary>
    public required ReplayMode Mode { get; init; }

    /// <summary>
    /// 加速倍数（仅在加速模式下有效，默认1.0）
    /// </summary>
    public double SpeedFactor { get; init; } = 1.0;

    /// <summary>
    /// 固定间隔毫秒（仅在固定间隔模式下有效，默认100ms）
    /// </summary>
    public int FixedIntervalMs { get; init; } = 100;
}

/// <summary>
/// 录制回放运行器接口
/// </summary>
public interface IRecordingReplayRunner
{
    /// <summary>
    /// 回放指定录制会话
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <param name="configuration">回放配置</param>
    /// <param name="ct">取消令牌</param>
    Task ReplayAsync(Guid sessionId, ReplayConfiguration configuration, CancellationToken ct = default);
}

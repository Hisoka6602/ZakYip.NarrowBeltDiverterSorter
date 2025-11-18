namespace ZakYip.NarrowBeltDiverterSorter.Host.Contracts.API;

/// <summary>
/// 长跑仿真启动响应。
/// </summary>
public class LongRunSimulationStartResponse
{
    /// <summary>
    /// 仿真运行 ID。
    /// </summary>
    public required string RunId { get; init; }

    /// <summary>
    /// 仿真状态。
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// 响应消息。
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// 仿真配置摘要。
    /// </summary>
    public required object Configuration { get; init; }
}

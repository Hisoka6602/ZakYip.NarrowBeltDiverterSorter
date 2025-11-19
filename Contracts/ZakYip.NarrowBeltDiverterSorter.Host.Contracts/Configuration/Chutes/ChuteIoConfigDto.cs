namespace ZakYip.NarrowBeltDiverterSorter.Host.Contracts.Configuration.Chutes;

/// <summary>
/// 格口 IO 配置传输模型。
/// </summary>
public sealed record ChuteIoConfigDto
{
    /// <summary>
    /// 格口Id。
    /// </summary>
    public required long ChuteId { get; init; }

    /// <summary>
    /// 总线标识（例如 Modbus 从站地址、继电器板Key等）。
    /// </summary>
    public required string BusKey { get; init; }

    /// <summary>
    /// 输出位索引（例如 0..31，对应Y1..Y32）。
    /// </summary>
    public required int OutputBitIndex { get; init; }

    /// <summary>
    /// 是否为常闭逻辑（true 表示发信时翻转为非默认态）。
    /// </summary>
    public bool IsNormallyOn { get; init; }
}

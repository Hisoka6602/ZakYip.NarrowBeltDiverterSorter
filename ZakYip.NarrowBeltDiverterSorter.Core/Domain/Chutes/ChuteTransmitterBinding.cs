namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.Chutes;

/// <summary>
/// 格口与发信器 IO 的映射配置。
/// </summary>
public sealed record ChuteTransmitterBinding
{
    /// <summary>
    /// 逻辑格口Id（业务视角的格口编号）。
    /// </summary>
    public required long ChuteId { get; init; }

    /// <summary>
    /// 总线标识（例如 Modbus 从站地址、继电器板Key等）。
    /// </summary>
    public required string BusKey { get; init; }

    /// <summary>
    /// 发信器输出位索引（例如 0..31，对应Y1..Y32）。
    /// </summary>
    public required int OutputBitIndex { get; init; }

    /// <summary>
    /// 输出是否为常闭逻辑（true 表示发信时翻转为非默认态）。
    /// </summary>
    public bool IsNormallyOn { get; init; }
}

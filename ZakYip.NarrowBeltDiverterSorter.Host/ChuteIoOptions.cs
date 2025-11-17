namespace ZakYip.NarrowBeltDiverterSorter.Host;

/// <summary>
/// 格口 IO 配置选项
/// </summary>
public sealed record ChuteIoOptions
{
    /// <summary>
    /// 配置节名称
    /// </summary>
    public const string SectionName = "ChuteIo";

    /// <summary>
    /// 是否启用真实格口IO
    /// </summary>
    public bool IsHardwareEnabled { get; init; }

    /// <summary>
    /// 运行模式：Simulation / ZhiQian32Relay / FutureBrand 等
    /// </summary>
    public required string Mode { get; init; }

    /// <summary>
    /// 多个 IP 节点定义
    /// </summary>
    public required IReadOnlyList<ChuteIoNodeOptions> Nodes { get; init; }
}

/// <summary>
/// 格口 IO 节点配置选项（一个节点对应一个 IP 端点）
/// </summary>
public sealed record ChuteIoNodeOptions
{
    /// <summary>
    /// 节点唯一键，例如 "zhiqian-node-1"
    /// </summary>
    public required string NodeKey { get; init; }

    /// <summary>
    /// 品牌标识，例如 "ZhiQian32Relay"、"Xinje" 等
    /// </summary>
    public required string Brand { get; init; }

    /// <summary>
    /// 目标 IP 地址
    /// </summary>
    public required string IpAddress { get; init; }

    /// <summary>
    /// TCP 端口，由配置提供，不写死默认值
    /// </summary>
    public int Port { get; init; }

    /// <summary>
    /// 该节点最大通道数，默认 32，用配置表达，不在代码写魔法数字
    /// </summary>
    public int MaxChannelCount { get; init; } = 32;

    /// <summary>
    /// 该节点下的格口绑定列表
    /// </summary>
    public required IReadOnlyList<ChuteChannelBindingOptions> Channels { get; init; }
}

/// <summary>
/// 格口通道绑定配置选项
/// </summary>
public sealed record ChuteChannelBindingOptions
{
    /// <summary>
    /// 格口ID
    /// </summary>
    public required long ChuteId { get; init; }

    /// <summary>
    /// 物理通道索引，1..MaxChannelCount
    /// </summary>
    public required int ChannelIndex { get; init; }
}

namespace ZakYip.NarrowBeltDiverterSorter.Core.Enums.Domain;

/// <summary>
/// 上游规则引擎连接状态
/// </summary>
public enum UpstreamConnectionStatus
{
    /// <summary>
    /// 已禁用（单机仿真模式）
    /// </summary>
    Disabled,

    /// <summary>
    /// 未连接
    /// </summary>
    Disconnected,

    /// <summary>
    /// 正在连接
    /// </summary>
    Connecting,

    /// <summary>
    /// 已连接
    /// </summary>
    Connected,

    /// <summary>
    /// 错误
    /// </summary>
    Error
}

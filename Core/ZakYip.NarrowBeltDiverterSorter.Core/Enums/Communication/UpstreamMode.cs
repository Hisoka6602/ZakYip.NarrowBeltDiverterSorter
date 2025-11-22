using System.ComponentModel;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Enums.Communication;

/// <summary>
/// 上游连接模式枚举
/// </summary>
public enum UpstreamMode
{
    /// <summary>
    /// 禁用上游连接（单机仿真模式）
    /// </summary>
    [Description("禁用")]
    Disabled,

    /// <summary>
    /// MQTT 协议连接
    /// </summary>
    [Description("MQTT")]
    Mqtt,

    /// <summary>
    /// TCP 协议连接
    /// </summary>
    [Description("TCP")]
    Tcp
}

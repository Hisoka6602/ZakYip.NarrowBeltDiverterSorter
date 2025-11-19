namespace ZakYip.NarrowBeltDiverterSorter.Communication.Upstream;

/// <summary>
/// 上游连接模式枚举
/// </summary>
public enum UpstreamMode
{
    /// <summary>
    /// 禁用上游连接（单机仿真模式）
    /// </summary>
    Disabled,

    /// <summary>
    /// MQTT 协议连接
    /// </summary>
    Mqtt,

    /// <summary>
    /// TCP 协议连接
    /// </summary>
    Tcp
}

/// <summary>
/// 上游连接配置选项
/// </summary>
public class UpstreamOptions
{
    /// <summary>
    /// 连接模式
    /// </summary>
    public UpstreamMode Mode { get; set; } = UpstreamMode.Disabled;

    /// <summary>
    /// MQTT 连接配置（当 Mode = Mqtt 时使用）
    /// </summary>
    public MqttOptions? Mqtt { get; set; }

    /// <summary>
    /// TCP 连接配置（当 Mode = Tcp 时使用）
    /// </summary>
    public TcpOptions? Tcp { get; set; }
}

/// <summary>
/// MQTT 连接配置
/// </summary>
public class MqttOptions
{
    /// <summary>
    /// Broker 地址
    /// </summary>
    public string Broker { get; set; } = "localhost";

    /// <summary>
    /// Broker 端口
    /// </summary>
    public int Port { get; set; } = 1883;

    /// <summary>
    /// 用户名（可选）
    /// </summary>
    public string? User { get; set; }

    /// <summary>
    /// 密码（可选）
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// 客户端ID（可选，默认自动生成）
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// 主题前缀（默认 "sorting"）
    /// </summary>
    public string BaseTopic { get; set; } = "sorting";

    /// <summary>
    /// 是否使用 TLS
    /// </summary>
    public bool UseTls { get; set; } = false;
}

/// <summary>
/// TCP 连接配置（预留）
/// </summary>
public class TcpOptions
{
    /// <summary>
    /// 服务器地址
    /// </summary>
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// 服务器端口
    /// </summary>
    public int Port { get; set; } = 8888;
}

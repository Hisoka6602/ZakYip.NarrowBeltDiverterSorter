namespace ZakYip.NarrowBeltDiverterSorter.Communication.Upstream;

/// <summary>
/// 上游模式枚举
/// </summary>
public enum UpstreamMode
{
    /// <summary>
    /// 禁用上游连接，本地仿真模式
    /// </summary>
    Disabled,

    /// <summary>
    /// MQTT 协议
    /// </summary>
    Mqtt,

    /// <summary>
    /// TCP 协议
    /// </summary>
    Tcp
}

/// <summary>
/// 上游配置选项
/// 用于配置与规则引擎的连接方式（非 HTTP）
/// </summary>
public class UpstreamOptions
{
    /// <summary>
    /// 上游模式：Disabled / Mqtt / Tcp
    /// </summary>
    public UpstreamMode Mode { get; set; } = UpstreamMode.Disabled;

    /// <summary>
    /// MQTT 配置
    /// </summary>
    public MqttConfiguration? Mqtt { get; set; }

    /// <summary>
    /// TCP 配置（预留）
    /// </summary>
    public TcpConfiguration? Tcp { get; set; }

    /// <summary>
    /// 默认格口编号（当上游不可用或禁用时使用）
    /// </summary>
    public int DefaultChuteNumber { get; set; } = 1;

    /// <summary>
    /// 创建默认配置
    /// </summary>
    public static UpstreamOptions CreateDefault()
    {
        return new UpstreamOptions
        {
            Mode = UpstreamMode.Disabled,
            DefaultChuteNumber = 1,
            Mqtt = MqttConfiguration.CreateDefault(),
            Tcp = TcpConfiguration.CreateDefault()
        };
    }
}

/// <summary>
/// MQTT 连接配置
/// </summary>
public class MqttConfiguration
{
    /// <summary>
    /// MQTT Broker 地址
    /// </summary>
    public string Broker { get; set; } = "localhost";

    /// <summary>
    /// MQTT Broker 端口
    /// </summary>
    public int Port { get; set; } = 1883;

    /// <summary>
    /// 用户名（可选）
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// 密码（可选）
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// 基础主题前缀
    /// </summary>
    public string BaseTopic { get; set; } = "sorting";

    /// <summary>
    /// 客户端ID前缀
    /// </summary>
    public string ClientIdPrefix { get; set; } = "narrowbelt";

    /// <summary>
    /// 连接超时（秒）
    /// </summary>
    public int ConnectionTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Keep Alive 间隔（秒）
    /// </summary>
    public int KeepAliveSeconds { get; set; } = 60;

    /// <summary>
    /// 创建默认配置
    /// </summary>
    public static MqttConfiguration CreateDefault()
    {
        return new MqttConfiguration
        {
            Broker = "localhost",
            Port = 1883,
            BaseTopic = "sorting",
            ClientIdPrefix = "narrowbelt",
            ConnectionTimeoutSeconds = 30,
            KeepAliveSeconds = 60
        };
    }
}

/// <summary>
/// TCP 连接配置（预留）
/// </summary>
public class TcpConfiguration
{
    /// <summary>
    /// TCP 服务器地址
    /// </summary>
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// TCP 服务器端口
    /// </summary>
    public int Port { get; set; } = 5000;

    /// <summary>
    /// 连接超时（秒）
    /// </summary>
    public int ConnectionTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// 创建默认配置
    /// </summary>
    public static TcpConfiguration CreateDefault()
    {
        return new TcpConfiguration
        {
            Host = "localhost",
            Port = 5000,
            ConnectionTimeoutSeconds = 30
        };
    }
}

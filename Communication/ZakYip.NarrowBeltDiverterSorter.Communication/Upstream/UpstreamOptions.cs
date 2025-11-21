using ZakYip.NarrowBeltDiverterSorter.Core.Enums.Communication;

namespace ZakYip.NarrowBeltDiverterSorter.Communication.Upstream;

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
    /// 通讯角色（客户端或服务端）
    /// </summary>
    public UpstreamRole Role { get; set; } = UpstreamRole.Client;

    /// <summary>
    /// MQTT 连接配置（当 Mode = Mqtt 时使用）
    /// </summary>
    public MqttOptions? Mqtt { get; set; }

    /// <summary>
    /// TCP 连接配置（当 Mode = Tcp 时使用）
    /// </summary>
    public TcpOptions? Tcp { get; set; }

    /// <summary>
    /// 连接重试配置（仅客户端模式有效）
    /// </summary>
    public RetryOptions Retry { get; set; } = new RetryOptions();
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

/// <summary>
/// 连接重试配置
/// </summary>
public class RetryOptions
{
    /// <summary>
    /// 初始退避间隔（毫秒）
    /// </summary>
    public int InitialBackoffMs { get; set; } = 100;

    /// <summary>
    /// 最大退避间隔（毫秒），默认 2000ms（2秒）
    /// </summary>
    public int MaxBackoffMs { get; set; } = 2000;

    /// <summary>
    /// 退避倍数（每次失败后退避时间乘以此倍数）
    /// </summary>
    public double BackoffMultiplier { get; set; } = 2.0;

    /// <summary>
    /// 是否启用无限重试（默认启用）
    /// </summary>
    public bool InfiniteRetry { get; set; } = true;
}

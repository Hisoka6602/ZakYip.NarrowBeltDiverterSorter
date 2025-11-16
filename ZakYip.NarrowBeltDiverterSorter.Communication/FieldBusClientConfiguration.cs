namespace ZakYip.NarrowBeltDiverterSorter.Communication;

/// <summary>
/// 现场总线客户端配置
/// </summary>
public class FieldBusClientConfiguration
{
    /// <summary>
    /// 服务器IP地址
    /// </summary>
    public string IpAddress { get; set; } = "127.0.0.1";

    /// <summary>
    /// 服务器端口
    /// </summary>
    public int Port { get; set; } = 502;

    /// <summary>
    /// 连接超时时间（毫秒）
    /// </summary>
    public int ConnectionTimeoutMs { get; set; } = 5000;

    /// <summary>
    /// 读写超时时间（毫秒）
    /// </summary>
    public int ReadWriteTimeoutMs { get; set; } = 3000;

    /// <summary>
    /// 从站ID（Modbus单元标识符）
    /// </summary>
    public byte SlaveId { get; set; } = 1;
}

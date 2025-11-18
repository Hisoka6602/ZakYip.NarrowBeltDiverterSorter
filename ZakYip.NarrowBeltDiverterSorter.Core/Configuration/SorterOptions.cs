namespace ZakYip.NarrowBeltDiverterSorter.Core.Configuration;

/// <summary>
/// Sorter 完整配置选项
/// 包含主线驱动模式选择和串口连接参数
/// </summary>
public sealed class SorterOptions
{
    /// <summary>
    /// 主线配置
    /// </summary>
    public SorterMainLineOptions MainLine { get; set; } = new();
}

/// <summary>
/// Sorter 主线配置选项
/// </summary>
public sealed class SorterMainLineOptions
{
    /// <summary>
    /// 主线驱动模式
    /// 可选值：Simulation（仿真）或 RemaLm1000H（真实硬件）
    /// 默认：Simulation
    /// </summary>
    public string Mode { get; set; } = "Simulation";

    /// <summary>
    /// Rema 串口连接配置
    /// 当 Mode 为 RemaLm1000H 时使用
    /// </summary>
    public RemaConnectionOptions Rema { get; set; } = new();
}

/// <summary>
/// Rema 串口连接配置选项
/// </summary>
public sealed class RemaConnectionOptions
{
    /// <summary>
    /// 串口号
    /// </summary>
    public string PortName { get; set; } = "COM3";

    /// <summary>
    /// 波特率
    /// </summary>
    public int BaudRate { get; set; } = 38400;

    /// <summary>
    /// 数据位
    /// </summary>
    public int DataBits { get; set; } = 8;

    /// <summary>
    /// 奇偶校验
    /// </summary>
    public string Parity { get; set; } = "None";

    /// <summary>
    /// 停止位
    /// </summary>
    public string StopBits { get; set; } = "One";

    /// <summary>
    /// Modbus 从站地址
    /// </summary>
    public int SlaveAddress { get; set; } = 1;

    /// <summary>
    /// 读取超时
    /// </summary>
    public TimeSpan ReadTimeout { get; set; } = TimeSpan.FromMilliseconds(1200);

    /// <summary>
    /// 写入超时
    /// </summary>
    public TimeSpan WriteTimeout { get; set; } = TimeSpan.FromMilliseconds(1200);

    /// <summary>
    /// 连接超时
    /// </summary>
    public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(3);

    /// <summary>
    /// 最大重试次数
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// 重试延迟
    /// </summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMilliseconds(200);
}

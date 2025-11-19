namespace ZakYip.NarrowBeltDiverterSorter.Execution.Vendors.Rema;

/// <summary>
/// 串口奇偶校验位
/// </summary>
public enum SerialParity
{
    /// <summary>无校验</summary>
    None = 0,
    /// <summary>奇校验</summary>
    Odd = 1,
    /// <summary>偶校验</summary>
    Even = 2,
    /// <summary>标记</summary>
    Mark = 3,
    /// <summary>空格</summary>
    Space = 4
}

/// <summary>
/// 串口停止位
/// </summary>
public enum SerialStopBits
{
    /// <summary>无停止位</summary>
    None = 0,
    /// <summary>1 个停止位</summary>
    One = 1,
    /// <summary>2 个停止位</summary>
    Two = 2,
    /// <summary>1.5 个停止位</summary>
    OnePointFive = 3
}

/// <summary>
/// 雷马 LM1000H Modbus RTU 连接配置参数
/// 参考：LM1000H 说明书 - Modbus 通讯参数
/// </summary>
public sealed record class RemaLm1000HConnectionOptions
{
    /// <summary>
    /// 串口号，例如 "COM1", "/dev/ttyS0"
    /// 参考：操作系统串口设备命名规范
    /// </summary>
    public required string PortName { get; init; }

    /// <summary>
    /// 波特率
    /// 参考：LM1000H 说明书 Pd.01 - 通讯波特率选择
    /// 支持值：600, 1200, 2400, 4800, 9600(默认), 19200, 38400, 57600, 115200
    /// </summary>
    public required int BaudRate { get; init; }

    /// <summary>
    /// 数据位数（通常为 8）
    /// 参考：LM1000H 说明书 Pd.02 - 数据格式
    /// </summary>
    public int DataBits { get; init; } = 8;

    /// <summary>
    /// 奇偶校验
    /// 参考：LM1000H 说明书 Pd.02 - 数据格式
    /// 0=8N2, 1=8E1, 2=8O1, 3=8N1(默认)
    /// </summary>
    public SerialParity Parity { get; init; } = SerialParity.None;

    /// <summary>
    /// 停止位
    /// 参考：LM1000H 说明书 Pd.02 - 数据格式
    /// </summary>
    public SerialStopBits StopBits { get; init; } = SerialStopBits.One;

    /// <summary>
    /// Modbus 从站地址（站号）
    /// 参考：LM1000H 说明书 - Modbus 从站地址设定
    /// 范围：1-247，默认值通常为 1
    /// </summary>
    public required byte SlaveAddress { get; init; }

    /// <summary>
    /// 读取操作超时时间
    /// 参考：Modbus RTU 协议规范
    /// 建议值：500-2000 毫秒
    /// </summary>
    public TimeSpan ReadTimeout { get; init; } = TimeSpan.FromMilliseconds(1000);

    /// <summary>
    /// 写入操作超时时间
    /// 参考：Modbus RTU 协议规范
    /// 建议值：500-2000 毫秒
    /// </summary>
    public TimeSpan WriteTimeout { get; init; } = TimeSpan.FromMilliseconds(1000);

    /// <summary>
    /// 连接超时时间
    /// 参考：Modbus RTU 协议规范
    /// 建议值：2000-5000 毫秒
    /// </summary>
    public TimeSpan ConnectTimeout { get; init; } = TimeSpan.FromMilliseconds(3000);

    /// <summary>
    /// 字符间超时时间（Modbus RTU 帧间隔）
    /// 参考：Modbus RTU 协议规范 - 1.5 字符时间
    /// 通常由底层库自动计算，此参数可选配置
    /// </summary>
    public TimeSpan? InterCharTimeout { get; init; }

    /// <summary>
    /// 通讯失败时的最大重试次数
    /// 建议值：2-3 次
    /// </summary>
    public int MaxRetries { get; init; } = 3;

    /// <summary>
    /// 重试间隔时间
    /// 建议值：100-500 毫秒
    /// </summary>
    public TimeSpan RetryDelay { get; init; } = TimeSpan.FromMilliseconds(200);
}

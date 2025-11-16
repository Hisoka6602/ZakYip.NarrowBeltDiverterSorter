namespace ZakYip.NarrowBeltDiverterSorter.Execution.MainLine.Rema;

/// <summary>
/// Modbus 客户端抽象接口
/// 封装底层 Modbus RTU/TCP 协议操作
/// 参考：Modbus 应用协议规范 V1.1b3
/// </summary>
public interface IModbusClient : IDisposable
{
    /// <summary>
    /// 连接到 Modbus 从站
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    Task<OperationResult> ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 断开与 Modbus 从站的连接
    /// </summary>
    Task<OperationResult> DisconnectAsync();

    /// <summary>
    /// 检查连接状态
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// 读取保持寄存器（功能码 0x03）
    /// </summary>
    /// <param name="slaveAddress">从站地址</param>
    /// <param name="startAddress">起始寄存器地址</param>
    /// <param name="count">读取数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>寄存器值数组</returns>
    Task<OperationResult<ushort[]>> ReadHoldingRegistersAsync(
        byte slaveAddress, 
        ushort startAddress, 
        ushort count, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 写入单个保持寄存器（功能码 0x06）
    /// </summary>
    /// <param name="slaveAddress">从站地址</param>
    /// <param name="registerAddress">寄存器地址</param>
    /// <param name="value">寄存器值</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task<OperationResult> WriteSingleRegisterAsync(
        byte slaveAddress, 
        ushort registerAddress, 
        ushort value, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 写入多个保持寄存器（功能码 0x10）
    /// </summary>
    /// <param name="slaveAddress">从站地址</param>
    /// <param name="startAddress">起始寄存器地址</param>
    /// <param name="values">寄存器值数组</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task<OperationResult> WriteMultipleRegistersAsync(
        byte slaveAddress, 
        ushort startAddress, 
        ushort[] values, 
        CancellationToken cancellationToken = default);
}

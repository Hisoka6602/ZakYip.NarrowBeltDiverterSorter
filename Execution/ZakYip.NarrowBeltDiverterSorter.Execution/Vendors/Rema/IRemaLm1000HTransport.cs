namespace ZakYip.NarrowBeltDiverterSorter.Execution.Vendors.Rema;

/// <summary>
/// 雷马 LM1000H 底层通讯抽象接口
/// 负责读写寄存器，不涉及业务逻辑
/// 
/// 实现说明：
/// - 开发/测试环境：使用 StubRemaLm1000HTransport（桩实现）
/// - 生产环境：需要实现 ModbusRemaLm1000HTransport，通过 Modbus RTU/TCP 协议与真实设备通信
///   可以使用 IFieldBusClient 或第三方 Modbus 库（如 NModbus4）来实现
/// </summary>
public interface IRemaLm1000HTransport
{
    /// <summary>
    /// 写入单个寄存器
    /// </summary>
    /// <param name="address">寄存器地址</param>
    /// <param name="value">寄存器值</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task WriteRegisterAsync(ushort address, ushort value, CancellationToken cancellationToken = default);

    /// <summary>
    /// 读取单个寄存器
    /// </summary>
    /// <param name="address">寄存器地址</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>寄存器值</returns>
    Task<ushort> ReadRegisterAsync(ushort address, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量写入多个寄存器
    /// </summary>
    /// <param name="startAddress">起始寄存器地址</param>
    /// <param name="values">寄存器值数组</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task WriteRegistersAsync(ushort startAddress, ushort[] values, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量读取多个寄存器
    /// </summary>
    /// <param name="startAddress">起始寄存器地址</param>
    /// <param name="count">读取数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>寄存器值数组</returns>
    Task<ushort[]> ReadRegistersAsync(ushort startAddress, ushort count, CancellationToken cancellationToken = default);
}

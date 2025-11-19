namespace ZakYip.NarrowBeltDiverterSorter.Communication;

/// <summary>
/// 现场总线客户端接口
/// 抽象 Modbus/TCP 或其他现场总线读写功能
/// </summary>
public interface IFieldBusClient
{
    /// <summary>
    /// 连接到现场总线
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否连接成功</returns>
    Task<bool> ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 断开现场总线连接
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    Task DisconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 写单个线圈
    /// </summary>
    /// <param name="address">线圈地址</param>
    /// <param name="value">线圈值</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否写入成功</returns>
    Task<bool> WriteSingleCoilAsync(int address, bool value, CancellationToken cancellationToken = default);

    /// <summary>
    /// 写多个线圈
    /// </summary>
    /// <param name="startAddress">起始地址</param>
    /// <param name="values">线圈值数组</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否写入成功</returns>
    Task<bool> WriteMultipleCoilsAsync(int startAddress, bool[] values, CancellationToken cancellationToken = default);

    /// <summary>
    /// 写单个保持寄存器
    /// </summary>
    /// <param name="address">寄存器地址</param>
    /// <param name="value">寄存器值</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否写入成功</returns>
    Task<bool> WriteSingleRegisterAsync(int address, ushort value, CancellationToken cancellationToken = default);

    /// <summary>
    /// 写多个保持寄存器
    /// </summary>
    /// <param name="startAddress">起始地址</param>
    /// <param name="values">寄存器值数组</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否写入成功</returns>
    Task<bool> WriteMultipleRegistersAsync(int startAddress, ushort[] values, CancellationToken cancellationToken = default);

    /// <summary>
    /// 读线圈状态
    /// </summary>
    /// <param name="address">线圈地址</param>
    /// <param name="count">读取数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>线圈状态数组，失败时返回null</returns>
    Task<bool[]?> ReadCoilsAsync(int address, int count, CancellationToken cancellationToken = default);

    /// <summary>
    /// 读离散输入状态
    /// </summary>
    /// <param name="address">输入地址</param>
    /// <param name="count">读取数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>输入状态数组，失败时返回null</returns>
    Task<bool[]?> ReadDiscreteInputsAsync(int address, int count, CancellationToken cancellationToken = default);

    /// <summary>
    /// 读保持寄存器
    /// </summary>
    /// <param name="address">寄存器地址</param>
    /// <param name="count">读取数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>寄存器值数组，失败时返回null</returns>
    Task<ushort[]?> ReadHoldingRegistersAsync(int address, int count, CancellationToken cancellationToken = default);

    /// <summary>
    /// 读输入寄存器
    /// </summary>
    /// <param name="address">寄存器地址</param>
    /// <param name="count">读取数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>寄存器值数组，失败时返回null</returns>
    Task<ushort[]?> ReadInputRegistersAsync(int address, int count, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查连接是否正常
    /// </summary>
    /// <returns>连接是否正常</returns>
    bool IsConnected();
}

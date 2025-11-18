namespace ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;

/// <summary>
/// IO输入源接口
/// 统一封装对底层IO（光电、编码器、继电器反馈等）的读取操作
/// </summary>
public interface IIoInputSource
{
    /// <summary>
    /// 读取离散输入状态
    /// </summary>
    /// <param name="address">IO地址</param>
    /// <param name="count">读取数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>IO状态数组，失败时返回null</returns>
    Task<bool[]?> ReadDiscreteInputsAsync(int address, int count, CancellationToken cancellationToken = default);

    /// <summary>
    /// 读取输入寄存器
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

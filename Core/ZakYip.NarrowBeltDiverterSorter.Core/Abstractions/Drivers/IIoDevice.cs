namespace ZakYip.NarrowBeltDiverterSorter.Core.Abstractions.Drivers;

/// <summary>
/// IO 设备接口
/// 提供通用 IO 设备控制的统一抽象（传感器、继电器、气缸等）
/// </summary>
public interface IIoDevice
{
    /// <summary>
    /// 设备标识
    /// </summary>
    string DeviceId { get; }

    /// <summary>
    /// 设备类型（Sensor, Relay, Cylinder 等）
    /// </summary>
    string DeviceType { get; }

    /// <summary>
    /// 初始化设备
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否初始化成功</returns>
    Task<bool> InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 关闭设备
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否关闭成功</returns>
    Task<bool> ShutdownAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 设备是否已就绪
    /// </summary>
    bool IsReady { get; }
}

namespace ZakYip.NarrowBeltDiverterSorter.Core.Abstractions.Drivers;

/// <summary>
/// 小车驱动接口
/// 提供小车运动控制的统一抽象，隔离具体厂商实现
/// </summary>
public interface ICartDrive
{
    /// <summary>
    /// 初始化小车驱动系统
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否初始化成功</returns>
    Task<bool> InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 关闭小车驱动系统
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否关闭成功</returns>
    Task<bool> ShutdownAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取小车环上的小车总数
    /// </summary>
    int TotalCartCount { get; }

    /// <summary>
    /// 驱动器是否已就绪
    /// </summary>
    bool IsReady { get; }
}

using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Chutes;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;

/// <summary>
/// 格口发信器端口接口（单连接，多格口）
/// 对应物理连接：单条通信连接，通过参数指定控制哪个格口
/// 注：底层只有一条通信连接，靠方法参数决定开哪个格口的窗口
/// </summary>
public interface IChuteTransmitterPort
{
    /// <summary>
    /// 打开指定格口的窗口
    /// </summary>
    /// <param name="chuteId">格口ID</param>
    /// <param name="openDuration">窗口打开时长</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    Task OpenWindowAsync(ChuteId chuteId, TimeSpan openDuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// 强制关闭指定格口的窗口
    /// </summary>
    /// <param name="chuteId">格口ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    Task ForceCloseAsync(ChuteId chuteId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取当前已注册的全部格口发信器绑定配置。
    /// </summary>
    IReadOnlyList<ChuteTransmitterBinding> GetRegisteredBindings();
}

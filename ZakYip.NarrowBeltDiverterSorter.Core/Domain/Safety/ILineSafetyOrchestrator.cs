using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Safety;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.Safety;

/// <summary>
/// 线体安全编排器接口
/// 负责统一管理线体的安全状态和运行状态，协调各子系统的安全响应
/// </summary>
public interface ILineSafetyOrchestrator
{
    /// <summary>
    /// 当前线体运行状态
    /// </summary>
    LineRunState CurrentLineRunState { get; }

    /// <summary>
    /// 当前安全状态
    /// </summary>
    SafetyState CurrentSafetyState { get; }

    /// <summary>
    /// 线体运行状态变化事件
    /// </summary>
    event EventHandler<LineRunStateChangedEventArgs>? LineRunStateChanged;

    /// <summary>
    /// 安全状态变化事件
    /// </summary>
    event EventHandler<SafetyStateChangedEventArgs>? SafetyStateChanged;

    /// <summary>
    /// 请求启动线体
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功启动或进入启动流程</returns>
    Task<bool> RequestStartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 请求正常停机
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功停止或进入停止流程</returns>
    Task<bool> RequestStopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 请求业务暂停
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功暂停</returns>
    Task<bool> RequestPauseAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 请求从暂停状态恢复运行
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功恢复</returns>
    Task<bool> RequestResumeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 确认故障，允许进入恢复流程
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功进入恢复流程</returns>
    Task<bool> AcknowledgeFaultAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 上报安全输入变化
    /// </summary>
    /// <param name="eventArgs">安全输入事件参数</param>
    void ReportSafetyInput(SafetyInputChangedEventArgs eventArgs);
}

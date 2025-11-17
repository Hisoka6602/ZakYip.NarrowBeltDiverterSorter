namespace ZakYip.NarrowBeltDiverterSorter.Observability.Recording;

/// <summary>
/// 事件录制管理器接口
/// 负责管理录制会话的生命周期
/// </summary>
public interface IEventRecordingManager
{
    /// <summary>
    /// 启动新的录制会话
    /// </summary>
    /// <param name="name">会话名称</param>
    /// <param name="description">会话描述（可选）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>录制会话信息</returns>
    Task<RecordingSessionInfo> StartSessionAsync(string name, string? description = null, CancellationToken ct = default);

    /// <summary>
    /// 停止指定的录制会话
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <param name="ct">取消令牌</param>
    Task StopSessionAsync(Guid sessionId, CancellationToken ct = default);

    /// <summary>
    /// 获取所有录制会话列表
    /// </summary>
    /// <param name="ct">取消令牌</param>
    /// <returns>录制会话信息列表</returns>
    Task<IReadOnlyList<RecordingSessionInfo>> ListSessionsAsync(CancellationToken ct = default);

    /// <summary>
    /// 获取指定会话的详细信息
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>录制会话信息，如果不存在则返回null</returns>
    Task<RecordingSessionInfo?> GetSessionAsync(Guid sessionId, CancellationToken ct = default);

    /// <summary>
    /// 获取当前活动的录制会话
    /// </summary>
    /// <returns>活动会话信息，如果没有活动会话则返回null</returns>
    RecordingSessionInfo? GetActiveSession();
}

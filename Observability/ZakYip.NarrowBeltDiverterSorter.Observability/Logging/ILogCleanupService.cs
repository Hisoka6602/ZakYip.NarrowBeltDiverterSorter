namespace ZakYip.NarrowBeltDiverterSorter.Observability.Logging;

/// <summary>
/// 日志清理服务接口
/// </summary>
public interface ILogCleanupService
{
    /// <summary>
    /// 清理过期日志文件
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>清理的文件数量</returns>
    Task<int> CleanupExpiredLogsAsync(CancellationToken cancellationToken = default);
}

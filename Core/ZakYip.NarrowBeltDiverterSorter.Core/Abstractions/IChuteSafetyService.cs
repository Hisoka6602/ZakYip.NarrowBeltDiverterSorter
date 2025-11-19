namespace ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;

/// <summary>
/// 格口安全控制服务接口
/// 提供全格口关闭能力，确保启动和停止时的安全保护
/// </summary>
public interface IChuteSafetyService
{
    /// <summary>
    /// 关闭全部格口发信器（发射 IO），确保不会有小车继续被触发。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    Task CloseAllChutesAsync(CancellationToken cancellationToken = default);
}

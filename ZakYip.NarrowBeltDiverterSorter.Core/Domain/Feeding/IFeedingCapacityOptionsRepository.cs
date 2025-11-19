namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.Feeding;

/// <summary>
/// 供包容量配置仓储接口
/// 提供供包容量控制配置的持久化访问
/// </summary>
public interface IFeedingCapacityOptionsRepository
{
    /// <summary>
    /// 加载供包容量配置
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>供包容量配置</returns>
    Task<FeedingCapacityOptions> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存供包容量配置
    /// </summary>
    /// <param name="options">供包容量配置</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task SaveAsync(FeedingCapacityOptions options, CancellationToken cancellationToken = default);
}

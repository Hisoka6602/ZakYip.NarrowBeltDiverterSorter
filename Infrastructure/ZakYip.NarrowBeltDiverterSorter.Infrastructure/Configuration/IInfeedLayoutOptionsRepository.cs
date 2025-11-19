using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Feeding;

namespace ZakYip.NarrowBeltDiverterSorter.Infrastructure.Configuration;

/// <summary>
/// 入口布局选项仓储接口
/// </summary>
public interface IInfeedLayoutOptionsRepository
{
    /// <summary>
    /// 异步加载入口布局选项
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>入口布局选项</returns>
    Task<InfeedLayoutOptions> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步保存入口布局选项
    /// </summary>
    /// <param name="options">入口布局选项</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task SaveAsync(InfeedLayoutOptions options, CancellationToken cancellationToken = default);
}

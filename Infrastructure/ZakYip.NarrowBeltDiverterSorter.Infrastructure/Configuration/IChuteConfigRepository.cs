using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Sorting;

namespace ZakYip.NarrowBeltDiverterSorter.Infrastructure.Configuration;

/// <summary>
/// 格口配置仓储接口
/// </summary>
public interface IChuteConfigRepository
{
    /// <summary>
    /// 异步加载格口配置集
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>格口配置集</returns>
    Task<ChuteConfigSet> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步保存格口配置集
    /// </summary>
    /// <param name="configSet">格口配置集</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task SaveAsync(ChuteConfigSet configSet, CancellationToken cancellationToken = default);
}

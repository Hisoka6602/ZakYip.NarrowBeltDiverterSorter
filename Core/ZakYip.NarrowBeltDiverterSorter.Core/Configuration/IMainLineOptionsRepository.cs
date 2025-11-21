using ZakYip.NarrowBeltDiverterSorter.Core.Domain.MainLine;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Configuration;

/// <summary>
/// 主线控制选项仓储接口
/// </summary>
public interface IMainLineOptionsRepository
{
    /// <summary>
    /// 异步加载主线控制选项
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>主线控制选项</returns>
    Task<MainLineControlOptions> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步保存主线控制选项
    /// </summary>
    /// <param name="options">主线控制选项</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task SaveAsync(MainLineControlOptions options, CancellationToken cancellationToken = default);
}

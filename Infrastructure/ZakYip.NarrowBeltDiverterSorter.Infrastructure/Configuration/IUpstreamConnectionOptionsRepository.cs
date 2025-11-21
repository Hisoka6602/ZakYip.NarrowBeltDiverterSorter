using ZakYip.NarrowBeltDiverterSorter.Communication.Upstream;

namespace ZakYip.NarrowBeltDiverterSorter.Infrastructure.Configuration;

/// <summary>
/// 上游连接选项仓储接口
/// </summary>
public interface IUpstreamConnectionOptionsRepository
{
    /// <summary>
    /// 异步加载上游连接选项
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>上游连接选项</returns>
    Task<UpstreamConnectionOptions> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步保存上游连接选项
    /// </summary>
    /// <param name="options">上游连接选项</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task SaveAsync(UpstreamConnectionOptions options, CancellationToken cancellationToken = default);
}

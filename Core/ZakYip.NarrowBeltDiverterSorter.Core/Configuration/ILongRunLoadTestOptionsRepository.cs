using ZakYip.NarrowBeltDiverterSorter.Core.Configuration;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Configuration;

/// <summary>
/// 长跑高负载测试选项仓储接口。
/// </summary>
public interface ILongRunLoadTestOptionsRepository
{
    /// <summary>
    /// 加载长跑测试选项。
    /// </summary>
    Task<LongRunLoadTestOptions> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存长跑测试选项。
    /// </summary>
    Task SaveAsync(LongRunLoadTestOptions options, CancellationToken cancellationToken = default);
}

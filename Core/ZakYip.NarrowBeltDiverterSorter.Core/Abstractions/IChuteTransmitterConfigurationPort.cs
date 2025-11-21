using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Chutes;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;

/// <summary>
/// 格口发信器配置端口（读写）。
/// </summary>
public interface IChuteTransmitterConfigurationPort
{
    /// <summary>
    /// 读取全部格口发信器绑定配置。
    /// </summary>
    Task<IReadOnlyList<ChuteTransmitterBinding>> GetAllBindingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新或插入格口发信器绑定配置。
    /// </summary>
    /// <param name="binding">绑定配置</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task UpsertBindingAsync(ChuteTransmitterBinding binding, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除指定格口的发信器绑定配置。
    /// </summary>
    /// <param name="chuteId">格口ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeleteBindingAsync(long chuteId, CancellationToken cancellationToken = default);
}

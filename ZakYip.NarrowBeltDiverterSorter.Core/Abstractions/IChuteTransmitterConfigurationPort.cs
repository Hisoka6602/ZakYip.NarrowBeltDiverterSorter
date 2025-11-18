using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Chutes;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;

/// <summary>
/// 格口发信器配置读取端口。
/// </summary>
public interface IChuteTransmitterConfigurationPort
{
    /// <summary>
    /// 读取全部格口发信器绑定配置。
    /// </summary>
    Task<IReadOnlyList<ChuteTransmitterBinding>> GetAllBindingsAsync(CancellationToken cancellationToken = default);
}

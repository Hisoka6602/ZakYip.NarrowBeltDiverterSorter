namespace ZakYip.NarrowBeltDiverterSorter.Core.Configuration;

/// <summary>
/// 面板 IO 联动选项仓储接口
/// </summary>
public interface IPanelIoLinkageOptionsRepository
{
    /// <summary>
    /// 异步加载面板 IO 联动选项
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>面板 IO 联动选项</returns>
    Task<PanelIoLinkageOptions> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步保存面板 IO 联动选项
    /// </summary>
    /// <param name="options">面板 IO 联动选项</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task SaveAsync(PanelIoLinkageOptions options, CancellationToken cancellationToken = default);
}

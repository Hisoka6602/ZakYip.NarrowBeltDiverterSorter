namespace ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;

/// <summary>
/// 通用格口 IO 服务接口
/// 按 ChuteId 维度操作，不直接暴露品牌细节
/// </summary>
public interface IChuteIoService
{
    /// <summary>
    /// 打开指定格口
    /// </summary>
    /// <param name="chuteId">格口ID</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>异步任务</returns>
    ValueTask OpenAsync(long chuteId, CancellationToken ct = default);

    /// <summary>
    /// 关闭指定格口
    /// </summary>
    /// <param name="chuteId">格口ID</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>异步任务</returns>
    ValueTask CloseAsync(long chuteId, CancellationToken ct = default);

    /// <summary>
    /// 关闭所有格口
    /// </summary>
    /// <param name="ct">取消令牌</param>
    /// <returns>异步任务</returns>
    ValueTask CloseAllAsync(CancellationToken ct = default);
}

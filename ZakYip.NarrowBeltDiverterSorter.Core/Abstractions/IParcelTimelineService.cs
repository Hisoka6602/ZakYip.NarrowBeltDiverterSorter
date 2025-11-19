using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Parcels;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;

/// <summary>
/// 包裹生命周期时间线服务接口
/// </summary>
public interface IParcelTimelineService
{
    /// <summary>
    /// 添加时间线事件
    /// </summary>
    /// <param name="eventArgs">事件参数</param>
    void Append(ParcelTimelineEventArgs eventArgs);

    /// <summary>
    /// 按包裹 ID 查询时间线事件
    /// </summary>
    /// <param name="parcelId">包裹 ID</param>
    /// <param name="maxCount">最大返回数量</param>
    /// <returns>事件列表（按时间升序）</returns>
    IReadOnlyList<ParcelTimelineEventArgs> QueryByParcel(long parcelId, int maxCount = 100);

    /// <summary>
    /// 查询最近的时间线事件
    /// </summary>
    /// <param name="maxCount">最大返回数量</param>
    /// <returns>事件列表（按时间降序）</returns>
    IReadOnlyList<ParcelTimelineEventArgs> QueryRecent(int maxCount = 100);
}

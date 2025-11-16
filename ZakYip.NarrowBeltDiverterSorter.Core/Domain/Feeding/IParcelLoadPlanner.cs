namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.Feeding;

/// <summary>
/// 包裹装载计划器接口
/// 预测包裹将被装载到哪个小车上
/// </summary>
public interface IParcelLoadPlanner
{
    /// <summary>
    /// 预测包裹将被装载到的小车
    /// </summary>
    /// <param name="infeedEdgeTime">入口触发时间</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>预测的小车ID，如果无法预测则返回null</returns>
    Task<CartId?> PredictLoadedCartAsync(DateTimeOffset infeedEdgeTime, CancellationToken ct);
}

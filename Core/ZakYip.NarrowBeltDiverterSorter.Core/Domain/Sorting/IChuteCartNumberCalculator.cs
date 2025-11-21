namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.Sorting;

/// <summary>
/// 格口小车号计算服务
/// 提供基于环形数组的格口窗口小车号计算逻辑
/// </summary>
public interface IChuteCartNumberCalculator
{
    /// <summary>
    /// 计算指定格口当前窗口的小车号
    /// </summary>
    /// <param name="totalCartCount">小车环上的总小车数量（必须 &gt; 0）</param>
    /// <param name="headCartNumber">当前原点处的小车号（1 基索引）</param>
    /// <param name="cartNumberWhenHeadAtOrigin">首车在原点时该格口窗口的小车号（1 基索引）</param>
    /// <returns>格口当前窗口的小车号（1 基索引）</returns>
    /// <exception cref="ArgumentException">当参数无效时抛出</exception>
    int GetCartNumberAtChute(
        int totalCartCount,
        int headCartNumber,
        int cartNumberWhenHeadAtOrigin);
}

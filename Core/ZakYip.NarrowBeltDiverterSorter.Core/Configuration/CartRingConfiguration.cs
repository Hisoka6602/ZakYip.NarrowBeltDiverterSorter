namespace ZakYip.NarrowBeltDiverterSorter.Core.Configuration;

/// <summary>
/// 小车环配置
/// 用于存储小车环相关的配置信息
/// </summary>
public sealed record CartRingConfiguration
{
    /// <summary>
    /// 小车环上的总小车数量
    /// &lt;= 0: 自动学习模式（尚未锁定）
    /// &gt; 0: 强制校验模式（已锁定）
    /// </summary>
    public int TotalCartCount { get; init; }

    /// <summary>
    /// 创建默认配置（自动学习模式）
    /// </summary>
    public static CartRingConfiguration CreateDefault()
    {
        return new CartRingConfiguration
        {
            TotalCartCount = 0 // 默认为自动学习模式
        };
    }
}

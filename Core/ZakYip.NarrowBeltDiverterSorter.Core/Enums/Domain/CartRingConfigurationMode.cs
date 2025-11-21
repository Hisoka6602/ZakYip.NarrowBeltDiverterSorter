namespace ZakYip.NarrowBeltDiverterSorter.Core.Enums.Domain;

/// <summary>
/// 小车环配置模式
/// </summary>
public enum CartRingConfigurationMode
{
    /// <summary>
    /// 自动学习模式（TotalCartCount &lt;= 0）
    /// </summary>
    AutoLearning,

    /// <summary>
    /// 校验模式（TotalCartCount &gt; 0）
    /// </summary>
    Verification
}

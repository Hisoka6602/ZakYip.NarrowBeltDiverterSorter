using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Tracking;

namespace ZakYip.NarrowBeltDiverterSorter.Core.SelfCheck;

/// <summary>
/// 小车环配置管理器接口
/// 负责根据自检结果自动学习或校验小车环配置
/// </summary>
public interface ICartRingConfigurationManager
{
    /// <summary>
    /// 处理自检结果
    /// 根据当前配置模式（自动学习或校验）执行相应的逻辑
    /// </summary>
    /// <param name="selfCheckResult">自检结果</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>处理结果，包含是否更新了配置、是否存在错误等信息</returns>
    Task<CartRingConfigurationProcessResult> ProcessSelfCheckResultAsync(
        CartRingSelfCheckResult selfCheckResult,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取当前小车环配置模式
    /// </summary>
    /// <returns>配置模式</returns>
    CartRingConfigurationMode GetCurrentMode();
}

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

/// <summary>
/// 小车环配置处理结果
/// </summary>
public sealed record CartRingConfigurationProcessResult
{
    /// <summary>
    /// 是否更新了配置
    /// </summary>
    public bool ConfigurationUpdated { get; init; }

    /// <summary>
    /// 是否存在错误
    /// </summary>
    public bool HasError { get; init; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// 处理的模式
    /// </summary>
    public CartRingConfigurationMode Mode { get; init; }

    /// <summary>
    /// 期望的小车数量
    /// </summary>
    public int ExpectedCartCount { get; init; }

    /// <summary>
    /// 实际检测到的小车数量
    /// </summary>
    public int DetectedCartCount { get; init; }
}

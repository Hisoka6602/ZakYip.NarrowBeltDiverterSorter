namespace ZakYip.NarrowBeltDiverterSorter.Core.Configuration;

/// <summary>
/// 小车环配置验证器
/// </summary>
public interface ICartRingConfigurationValidator
{
    /// <summary>
    /// 验证配置是否有效
    /// </summary>
    /// <param name="configuration">要验证的配置</param>
    /// <returns>验证结果</returns>
    CartRingConfigurationValidationResult Validate(CartRingConfiguration configuration);
}

/// <summary>
/// 配置验证结果
/// </summary>
public sealed record CartRingConfigurationValidationResult
{
    /// <summary>
    /// 是否有效
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// 错误消息列表
    /// </summary>
    public IReadOnlyList<string> ErrorMessages { get; init; } = Array.Empty<string>();

    /// <summary>
    /// 创建成功结果
    /// </summary>
    public static CartRingConfigurationValidationResult Success()
    {
        return new CartRingConfigurationValidationResult { IsValid = true };
    }

    /// <summary>
    /// 创建失败结果
    /// </summary>
    public static CartRingConfigurationValidationResult Failure(params string[] errorMessages)
    {
        return new CartRingConfigurationValidationResult
        {
            IsValid = false,
            ErrorMessages = errorMessages
        };
    }
}

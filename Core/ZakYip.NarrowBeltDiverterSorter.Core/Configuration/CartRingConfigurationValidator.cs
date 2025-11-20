namespace ZakYip.NarrowBeltDiverterSorter.Core.Configuration;

/// <summary>
/// 小车环配置验证器实现
/// </summary>
public sealed class CartRingConfigurationValidator : ICartRingConfigurationValidator
{
    /// <inheritdoc/>
    public CartRingConfigurationValidationResult Validate(CartRingConfiguration configuration)
    {
        if (configuration == null)
        {
            return CartRingConfigurationValidationResult.Failure("配置不能为空");
        }

        // TotalCartCount 可以是 <= 0（自动学习模式）或 > 0（校验模式）
        // 没有特别的验证要求

        return CartRingConfigurationValidationResult.Success();
    }
}

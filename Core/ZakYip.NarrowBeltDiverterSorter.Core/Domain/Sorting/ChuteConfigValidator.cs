namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.Sorting;

/// <summary>
/// 格口配置验证器实现
/// </summary>
public sealed class ChuteConfigValidator : IChuteConfigValidator
{
    /// <inheritdoc/>
    public ChuteConfigValidationResult Validate(ChuteConfig config, int totalCartCount)
    {
        if (config == null)
        {
            return ChuteConfigValidationResult.Failure("格口配置不能为空");
        }

        // 如果 TotalCartCount > 0（校验模式），则 CartNumberWhenHeadAtOrigin 必须在有效范围内
        if (totalCartCount > 0)
        {
            if (config.CartNumberWhenHeadAtOrigin <= 0)
            {
                return ChuteConfigValidationResult.Failure(
                    $"格口 {config.ChuteId.Value} 的窗口小车号必须大于 0，当前值：{config.CartNumberWhenHeadAtOrigin}");
            }

            if (config.CartNumberWhenHeadAtOrigin > totalCartCount)
            {
                return ChuteConfigValidationResult.Failure(
                    $"格口 {config.ChuteId.Value} 的窗口小车号必须在 1 和总小车数量 {totalCartCount} 之间，当前值：{config.CartNumberWhenHeadAtOrigin}");
            }
        }

        return ChuteConfigValidationResult.Success();
    }
}

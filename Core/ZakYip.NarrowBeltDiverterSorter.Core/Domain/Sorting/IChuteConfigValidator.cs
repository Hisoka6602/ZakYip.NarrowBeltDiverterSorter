namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.Sorting;

/// <summary>
/// 格口配置验证器
/// </summary>
public interface IChuteConfigValidator
{
    /// <summary>
    /// 验证格口配置
    /// </summary>
    /// <param name="config">格口配置</param>
    /// <param name="totalCartCount">小车环总数量（用于验证 CartNumberWhenHeadAtOrigin）</param>
    /// <returns>验证结果</returns>
    ChuteConfigValidationResult Validate(ChuteConfig config, int totalCartCount);
}

/// <summary>
/// 格口配置验证结果
/// </summary>
public sealed record ChuteConfigValidationResult
{
    /// <summary>
    /// 是否有效
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// 创建成功结果
    /// </summary>
    public static ChuteConfigValidationResult Success()
    {
        return new ChuteConfigValidationResult { IsValid = true };
    }

    /// <summary>
    /// 创建失败结果
    /// </summary>
    public static ChuteConfigValidationResult Failure(string errorMessage)
    {
        return new ChuteConfigValidationResult
        {
            IsValid = false,
            ErrorMessage = errorMessage
        };
    }
}

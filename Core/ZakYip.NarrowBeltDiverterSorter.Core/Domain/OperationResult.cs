namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain;

/// <summary>
/// 操作结果
/// </summary>
public record class OperationResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// 错误消息（失败时）
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// 创建成功结果
    /// </summary>
    public static OperationResult Success() => new() { IsSuccess = true };

    /// <summary>
    /// 创建失败结果
    /// </summary>
    public static OperationResult Failure(string errorMessage) => new()
    {
        IsSuccess = false,
        ErrorMessage = errorMessage
    };
}

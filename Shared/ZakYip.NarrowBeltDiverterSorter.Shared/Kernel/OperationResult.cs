namespace ZakYip.NarrowBeltDiverterSorter.Shared.Kernel;

/// <summary>
/// 通用操作结果模式
/// 用于封装操作的成功/失败状态及错误消息
/// </summary>
/// <remarks>
/// 此类为不可变记录类型，线程安全且适合函数式编程风格。
/// 使用静态工厂方法创建成功或失败的结果实例。
/// </remarks>
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
    /// <returns>表示成功的操作结果</returns>
    public static OperationResult Success() => new() { IsSuccess = true };

    /// <summary>
    /// 创建失败结果
    /// </summary>
    /// <param name="errorMessage">错误消息，描述失败原因</param>
    /// <returns>表示失败的操作结果</returns>
    public static OperationResult Failure(string errorMessage) => new()
    {
        IsSuccess = false,
        ErrorMessage = errorMessage
    };
}

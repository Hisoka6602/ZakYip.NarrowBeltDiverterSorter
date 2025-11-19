namespace ZakYip.NarrowBeltDiverterSorter.Execution.Vendors.Rema;

/// <summary>
/// 表示一个操作的执行结果
/// 用于封装通讯操作的成功/失败状态，避免异常传播到调用者
/// </summary>
public readonly struct OperationResult
{
    /// <summary>
    /// 操作是否成功
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// 错误消息（仅当 IsSuccess = false 时有效）
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// 内部异常（仅当 IsSuccess = false 时有效）
    /// </summary>
    public Exception? Exception { get; }

    private OperationResult(bool isSuccess, string? errorMessage = null, Exception? exception = null)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        Exception = exception;
    }

    /// <summary>
    /// 创建成功结果
    /// </summary>
    public static OperationResult Success() => new(true);

    /// <summary>
    /// 创建失败结果
    /// </summary>
    /// <param name="errorMessage">错误消息</param>
    /// <param name="exception">异常对象（可选）</param>
    public static OperationResult Failure(string errorMessage, Exception? exception = null) 
        => new(false, errorMessage, exception);
}

/// <summary>
/// 表示一个操作的执行结果，携带返回值
/// </summary>
/// <typeparam name="T">返回值类型</typeparam>
public readonly struct OperationResult<T>
{
    /// <summary>
    /// 操作是否成功
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// 返回值（仅当 IsSuccess = true 时有效）
    /// </summary>
    public T? Value { get; }

    /// <summary>
    /// 错误消息（仅当 IsSuccess = false 时有效）
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// 内部异常（仅当 IsSuccess = false 时有效）
    /// </summary>
    public Exception? Exception { get; }

    private OperationResult(bool isSuccess, T? value = default, string? errorMessage = null, Exception? exception = null)
    {
        IsSuccess = isSuccess;
        Value = value;
        ErrorMessage = errorMessage;
        Exception = exception;
    }

    /// <summary>
    /// 创建成功结果
    /// </summary>
    /// <param name="value">返回值</param>
    public static OperationResult<T> Success(T value) => new(true, value);

    /// <summary>
    /// 创建失败结果
    /// </summary>
    /// <param name="errorMessage">错误消息</param>
    /// <param name="exception">异常对象（可选）</param>
    public static OperationResult<T> Failure(string errorMessage, Exception? exception = null) 
        => new(false, default, errorMessage, exception);
}

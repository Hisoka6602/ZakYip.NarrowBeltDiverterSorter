using Microsoft.Extensions.Logging;

namespace ZakYip.NarrowBeltDiverterSorter.Shared.Kernel;

/// <summary>
/// 统一安全隔离器
/// 用于包装可能抛出异常的操作，确保异常被捕获和记录，不会导致进程崩溃
/// </summary>
public class SafetyIsolator
{
    private readonly ILogger<SafetyIsolator> _logger;

    public SafetyIsolator(ILogger<SafetyIsolator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 安全执行同步操作
    /// </summary>
    /// <param name="action">要执行的操作</param>
    /// <param name="operationName">操作名称（用于日志）</param>
    /// <param name="context">操作上下文（用于日志）</param>
    /// <returns>操作是否成功</returns>
    public bool Execute(Action action, string operationName, string? context = null)
    {
        try
        {
            action();
            return true;
        }
        catch (Exception ex)
        {
            LogException(ex, operationName, context);
            return false;
        }
    }

    /// <summary>
    /// 安全执行同步操作并返回结果
    /// </summary>
    /// <typeparam name="T">返回类型</typeparam>
    /// <param name="func">要执行的操作</param>
    /// <param name="defaultValue">失败时返回的默认值</param>
    /// <param name="operationName">操作名称（用于日志）</param>
    /// <param name="context">操作上下文（用于日志）</param>
    /// <returns>操作结果或默认值</returns>
    public T Execute<T>(Func<T> func, T defaultValue, string operationName, string? context = null)
    {
        try
        {
            return func();
        }
        catch (Exception ex)
        {
            LogException(ex, operationName, context);
            return defaultValue;
        }
    }

    /// <summary>
    /// 安全执行异步操作
    /// </summary>
    /// <param name="asyncAction">要执行的异步操作</param>
    /// <param name="operationName">操作名称（用于日志）</param>
    /// <param name="context">操作上下文（用于日志）</param>
    /// <returns>操作是否成功</returns>
    public async Task<bool> ExecuteAsync(Func<Task> asyncAction, string operationName, string? context = null)
    {
        try
        {
            await asyncAction();
            return true;
        }
        catch (Exception ex)
        {
            LogException(ex, operationName, context);
            return false;
        }
    }

    /// <summary>
    /// 安全执行异步操作并返回结果
    /// </summary>
    /// <typeparam name="T">返回类型</typeparam>
    /// <param name="asyncFunc">要执行的异步操作</param>
    /// <param name="defaultValue">失败时返回的默认值</param>
    /// <param name="operationName">操作名称（用于日志）</param>
    /// <param name="context">操作上下文（用于日志）</param>
    /// <returns>操作结果或默认值</returns>
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> asyncFunc, T defaultValue, string operationName, string? context = null)
    {
        try
        {
            return await asyncFunc();
        }
        catch (Exception ex)
        {
            LogException(ex, operationName, context);
            return defaultValue;
        }
    }

    private void LogException(Exception ex, string operationName, string? context)
    {
        if (string.IsNullOrEmpty(context))
        {
            _logger.LogError(ex, "安全隔离器捕获异常 - 操作: {OperationName}", operationName);
        }
        else
        {
            _logger.LogError(ex, "安全隔离器捕获异常 - 操作: {OperationName}, 上下文: {Context}", 
                operationName, context);
        }
    }
}

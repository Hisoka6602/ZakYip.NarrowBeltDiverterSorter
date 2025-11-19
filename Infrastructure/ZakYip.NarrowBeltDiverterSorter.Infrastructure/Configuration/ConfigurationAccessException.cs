namespace ZakYip.NarrowBeltDiverterSorter.Infrastructure.Configuration;

/// <summary>
/// 配置访问异常
/// </summary>
public class ConfigurationAccessException : Exception
{
    /// <summary>
    /// 初始化配置访问异常
    /// </summary>
    public ConfigurationAccessException()
    {
    }

    /// <summary>
    /// 初始化配置访问异常，并指定异常消息
    /// </summary>
    /// <param name="message">异常消息</param>
    public ConfigurationAccessException(string message) : base(message)
    {
    }

    /// <summary>
    /// 初始化配置访问异常，并指定异常消息和内部异常
    /// </summary>
    /// <param name="message">异常消息</param>
    /// <param name="innerException">内部异常</param>
    public ConfigurationAccessException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

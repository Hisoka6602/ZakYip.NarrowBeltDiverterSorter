using System.Text.Json.Serialization;

namespace ZakYip.NarrowBeltDiverterSorter.Host.DTOs;

/// <summary>
/// 统一的 API 响应结果模型
/// </summary>
/// <typeparam name="T">响应数据类型</typeparam>
public sealed record ApiResult<T>
{
    /// <summary>
    /// 是否成功
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    /// <summary>
    /// 响应消息
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; init; }

    /// <summary>
    /// 响应数据
    /// </summary>
    [JsonPropertyName("data")]
    public T? Data { get; init; }

    /// <summary>
    /// 错误代码（失败时）
    /// </summary>
    [JsonPropertyName("errorCode")]
    public string? ErrorCode { get; init; }

    /// <summary>
    /// 错误详细信息（失败时）
    /// </summary>
    [JsonPropertyName("errors")]
    public Dictionary<string, string[]>? Errors { get; init; }

    /// <summary>
    /// 时间戳
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.Now;

    /// <summary>
    /// 创建成功响应
    /// </summary>
    public static ApiResult<T> Ok(T data, string? message = null)
    {
        return new ApiResult<T>
        {
            Success = true,
            Data = data,
            Message = message
        };
    }

    /// <summary>
    /// 创建失败响应
    /// </summary>
    public static ApiResult<T> Fail(string message, string? errorCode = null, Dictionary<string, string[]>? errors = null)
    {
        return new ApiResult<T>
        {
            Success = false,
            Message = message,
            ErrorCode = errorCode,
            Errors = errors
        };
    }
}

/// <summary>
/// 无数据的 API 响应结果模型
/// </summary>
public sealed record ApiResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    /// <summary>
    /// 响应消息
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; init; }

    /// <summary>
    /// 错误代码（失败时）
    /// </summary>
    [JsonPropertyName("errorCode")]
    public string? ErrorCode { get; init; }

    /// <summary>
    /// 错误详细信息（失败时）
    /// </summary>
    [JsonPropertyName("errors")]
    public Dictionary<string, string[]>? Errors { get; init; }

    /// <summary>
    /// 时间戳
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.Now;

    /// <summary>
    /// 创建成功响应
    /// </summary>
    public static ApiResult Ok(string? message = null)
    {
        return new ApiResult
        {
            Success = true,
            Message = message
        };
    }

    /// <summary>
    /// 创建失败响应
    /// </summary>
    public static ApiResult Fail(string message, string? errorCode = null, Dictionary<string, string[]>? errors = null)
    {
        return new ApiResult
        {
            Success = false,
            Message = message,
            ErrorCode = errorCode,
            Errors = errors
        };
    }
}

namespace ZakYip.NarrowBeltDiverterSorter.Host.DTOs;

/// <summary>
/// 上游连接选项 DTO。
/// </summary>
public sealed record UpstreamConnectionOptionsDto
{
    /// <summary>
    /// 上游 API 基础 URL。
    /// </summary>
    public required string BaseUrl { get; init; }

    /// <summary>
    /// 请求超时时间（秒）。
    /// </summary>
    public required int RequestTimeoutSeconds { get; init; }

    /// <summary>
    /// 认证令牌（可选）。
    /// </summary>
    public string? AuthToken { get; init; }
}

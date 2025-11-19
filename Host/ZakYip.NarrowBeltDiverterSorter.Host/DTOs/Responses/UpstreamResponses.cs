namespace ZakYip.NarrowBeltDiverterSorter.Host.DTOs.Responses;

/// <summary>
/// 测试包裹响应
/// </summary>
public sealed record TestParcelResponse
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// 响应消息
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// 包裹ID
    /// </summary>
    public string ParcelId { get; init; } = string.Empty;

    /// <summary>
    /// 包裹条码
    /// </summary>
    public string Barcode { get; init; } = string.Empty;

    /// <summary>
    /// 发送时间
    /// </summary>
    public DateTimeOffset SentAt { get; init; }
}

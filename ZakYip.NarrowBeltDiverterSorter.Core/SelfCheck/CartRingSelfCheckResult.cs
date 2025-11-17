namespace ZakYip.NarrowBeltDiverterSorter.Core.SelfCheck;

/// <summary>
/// 小车环自检结果
/// </summary>
public sealed record CartRingSelfCheckResult
{
    /// <summary>
    /// 配置的小车数量
    /// </summary>
    public required int ExpectedCartCount { get; init; }

    /// <summary>
    /// 检测到的小车数量
    /// </summary>
    public required int MeasuredCartCount { get; init; }

    /// <summary>
    /// 配置的节距（mm）
    /// </summary>
    public required decimal ExpectedPitchMm { get; init; }

    /// <summary>
    /// 估算出的节距（mm）
    /// </summary>
    public required decimal MeasuredPitchMm { get; init; }

    /// <summary>
    /// 小车数量是否匹配
    /// </summary>
    public required bool IsCartCountMatched { get; init; }

    /// <summary>
    /// 节距是否在容忍范围内
    /// </summary>
    public required bool IsPitchWithinTolerance { get; init; }
}

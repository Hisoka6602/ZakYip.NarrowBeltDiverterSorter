using ZakYip.NarrowBeltDiverterSorter.Core.Enums;
using ZakYip.NarrowBeltDiverterSorter.Core.Enums.Domain;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.Safety;

/// <summary>
/// 安全输入变化事件参数（核心层接口使用）
/// </summary>
public record class SafetyInputChangedEventArgs
{
    /// <summary>
    /// 安全输入源标识（例如"EStop1"、"SafetyDoor"）
    /// </summary>
    public required string Source { get; init; }

    /// <summary>
    /// 安全输入类型
    /// </summary>
    public required SafetyInputType InputType { get; init; }

    /// <summary>
    /// 是否激活（true=安全，false=不安全）
    /// </summary>
    public required bool IsActive { get; init; }

    /// <summary>
    /// 事件发生时间（本地时间）
    /// </summary>
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.Now;
}

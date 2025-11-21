using ZakYip.NarrowBeltDiverterSorter.Core.Enums.Domain;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.Safety;

/// <summary>
/// 安全输入变化事件参数
/// </summary>
public record class SafetyInputChangedEventArgs
{
    /// <summary>
    /// 输入源标识（例如"EmergencyStop1"、"SafetyDoor2"）
    /// </summary>
    public required string Source { get; init; }

    /// <summary>
    /// 输入类型
    /// </summary>
    public required SafetyInputType InputType { get; init; }

    /// <summary>
    /// 输入值（true表示安全/正常，false表示不安全/触发）
    /// </summary>
    public required bool IsActive { get; init; }

    /// <summary>
    /// 事件发生时间（本地时间）
    /// </summary>
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.Now;
}

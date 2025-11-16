namespace ZakYip.NarrowBeltDiverterSorter.Execution.MainLine;

/// <summary>
/// 主驱动线控制接口。
/// - 负责设置目标线速；
/// - 负责读取当前线速；
/// - 对外暴露"是否稳定"等状态。
/// </summary>
public interface IMainLineDrive
{
    /// <summary>设置目标线速，单位：mm/s。</summary>
    Task SetTargetSpeedAsync(decimal targetSpeedMmps, CancellationToken cancellationToken = default);

    /// <summary>当前实际线速，单位：mm/s。</summary>
    decimal CurrentSpeedMmps { get; }

    /// <summary>当前目标线速，单位：mm/s。</summary>
    decimal TargetSpeedMmps { get; }

    /// <summary>当前是否满足"稳速"判定。</summary>
    bool IsSpeedStable { get; }
}

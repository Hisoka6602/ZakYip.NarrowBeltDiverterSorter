namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.MainLine;

/// <summary>
/// 主线设定点提供者接口
/// 用于暴露主线"期望速度"和"是否允许运行"的状态，不直接操作硬件
/// </summary>
public interface IMainLineSetpointProvider
{
    /// <summary>
    /// 是否允许运行
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// 目标速度（mm/s）
    /// </summary>
    decimal TargetMmps { get; }
}

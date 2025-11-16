namespace ZakYip.NarrowBeltDiverterSorter.Host;

/// <summary>
/// 主线驱动实现类型
/// </summary>
public enum MainLineDriveImplementation
{
    /// <summary>
    /// 仿真主线驱动
    /// </summary>
    Simulation,
    
    /// <summary>
    /// 雷马 LM1000H 主线驱动
    /// </summary>
    RemaLm1000H
}

/// <summary>
/// 主线驱动配置选项
/// 用于在 Host 层选择主线驱动实现
/// </summary>
public sealed class MainLineDriveOptions
{
    /// <summary>
    /// 配置节名称
    /// </summary>
    public const string SectionName = "Sorter:MainLine";

    /// <summary>
    /// 主线驱动实现类型
    /// 可选值：Simulation, RemaLm1000H
    /// 默认：Simulation
    /// </summary>
    public MainLineDriveImplementation Implementation { get; set; } = MainLineDriveImplementation.Simulation;

    /// <summary>
    /// 获取实现类型的中文描述
    /// </summary>
    public string GetImplementationDescription() => Implementation switch
    {
        MainLineDriveImplementation.Simulation => "仿真主线",
        MainLineDriveImplementation.RemaLm1000H => "Rema LM1000H",
        _ => "未知"
    };
}

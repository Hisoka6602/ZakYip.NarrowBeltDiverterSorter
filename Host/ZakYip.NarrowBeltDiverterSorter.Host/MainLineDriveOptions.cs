using ZakYip.NarrowBeltDiverterSorter.Execution.Vendors.Rema;

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
    /// 主线驱动模式（Implementation 的别名，支持配置中使用 Mode 字段）
    /// 可选值：Simulation, RemaLm1000H
    /// 默认：Simulation
    /// </summary>
    public string? Mode 
    { 
        get => Implementation.ToString();
        set 
        {
            if (!string.IsNullOrEmpty(value) && Enum.TryParse<MainLineDriveImplementation>(value, true, out var result))
            {
                Implementation = result;
            }
        }
    }

    /// <summary>
    /// 雷马 LM1000H 连接配置
    /// 当 Mode 为 RemaLm1000H 时使用
    /// </summary>
    public RemaLm1000HConnectionOptions? Rema { get; set; }

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

namespace ZakYip.NarrowBeltDiverterSorter.Core.Configuration;

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
/// 用于选择主线驱动实现
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
    /// 注意：此属性类型为 object 以避免 Core 层依赖 Execution 层
    /// 实际使用时应转换为 RemaLm1000HConnectionOptions 类型
    /// </summary>
    public object? Rema { get; set; }

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
